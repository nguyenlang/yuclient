using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using NLog;

namespace RemoteCenter.Utils
{

    /// <summary>
    /// Allows commands to be executed at the command line and retrieve any output sent
    /// to both the standard and error output streams.
    /// </summary>
    public class CommandLineExecuter
    {
        #region Fields

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly StringBuilder _output = new StringBuilder();
        private bool _isProcessing;

        #endregion

        #region Properties

        /// <summary>
        /// The path to the application that should be executed
        /// </summary>
        public string ApplicationPath { private get; set; }

        /// <summary>
        /// Command line parameters
        /// </summary>
        public string Arguments { private get; set; }

        /// <summary>
        /// Gets the output after the command line has been executed
        /// </summary>
        public string Output
        {
            get
            {
                return _output.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the output targets controlling where console output is sent
        /// </summary>
        public OutputTarget OutputTargets { get; set; }

        /// <summary>
        /// Gets or sets the path where the output should be stored
        /// </summary>
        public string OutputFile { get; set; }

        /// <summary>
        /// Gets the exit code after the command line has been executed
        /// </summary>
        public int ExitCode { private set; get; }

        /// <summary>
        /// Gets or sets the processor affinity to use for the command line application
        /// </summary>
        public static int ProcessorAffinity { get; set; }

        private bool UseAsyncMode
        {
            get
            {
                return string.IsNullOrEmpty(OutputFile);
            }
        }

        #endregion

        #region Constructor

        public CommandLineExecuter()
        {
            OutputTargets = OutputTarget.All;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Executes the command line and waits until all output has been retrieved.
        /// </summary>
        /// <param name="timeout">Timeout in milliseconds</param>
        public void Go(int timeout = 60000)
        {
            Process process = Start();

            if (UseAsyncMode)
            {
                process.OutputDataReceived += OutputDataReceived;
                process.ErrorDataReceived += OutputDataReceived;
                process.Exited += ProcessExited;

                _isProcessing = true;
            }

            process.Start();

            SetOutput(process, timeout);
        }

        private Process Start()
        {
            _logger.Debug("Executing command line: {0} {1}", ApplicationPath, Arguments);

            Process process = GetProcess();

            return process;
        }

        private Process GetProcess()
        {
            if (string.IsNullOrEmpty(ApplicationPath))
                throw new SystemException("Unable to execute command line, application path has not been specified");

            if (!File.Exists(ApplicationPath))
                throw new SystemException("Unable to execute command line, application path does not exist: " + ApplicationPath);

            var process = new Process
            {
                EnableRaisingEvents = UseAsyncMode,
                StartInfo =
                                    {
                                        RedirectStandardOutput = true,
                                        RedirectStandardError = true,
                                        UseShellExecute = false,
                                        CreateNoWindow = true,
                                        FileName = ApplicationPath,
                                        Arguments = Arguments
                                    }
            };

            string workingDirectory = Path.GetDirectoryName(ApplicationPath);

            if (workingDirectory != null)
                process.StartInfo.WorkingDirectory = workingDirectory;

            return process;
        }

        private void SetOutput(Process process, int timeout)
        {
            if (string.IsNullOrEmpty(OutputFile))
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            if (ProcessorAffinity > 0)
            {
                process.ProcessorAffinity = (IntPtr)ProcessorAffinity;
                _logger.Debug("Changed processor affinity to {0} for process: {1}, id: {2}", process.ProcessorAffinity, process.ProcessName, process.Id);
            }

            if (UseAsyncMode)
            {
                while (_isProcessing)
                {
                    TimeSpan timeProcessing = (DateTime.Now - process.StartTime);

                    if (timeout > 0 && timeProcessing.TotalMilliseconds >= timeout)
                    {
                        _isProcessing = false;

                        process.Kill();

                        _logger.Error("Command line failed, timeout of {0} ms reached", timeout);

                        return;
                    }
                    else
                    {
                        Thread.Sleep(0);
                    }
                }
            }

            if (!string.IsNullOrEmpty(OutputFile))
            {
                using (Stream stream = File.OpenWrite(OutputFile))
                {
                    CopyStream(process.StandardOutput.BaseStream, stream);
                }
            }

            _logger.Debug("Command line exit code: {0}", process.ExitCode);
            ExitCode = process.ExitCode;
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Executes the command line and gets the response
        /// </summary>
        public static string Execute(string app, string args)
        {
            int exitCode;
            return Execute(app, args, out exitCode);
        }

        /// <summary>
        /// Executes the command line and gets the response and exit code
        /// </summary>
        public static string Execute(string app, string args, out int exitCode)
        {
            var cle = new CommandLineExecuter { ApplicationPath = app, Arguments = args };
            cle.Go();
            exitCode = cle.ExitCode;
            return cle.Output;
        }

        /// <summary>
        /// Executes the command line asynchronously and does not return the response
        /// </summary>
        public void ExecuteAsync()
        {
            var thread = new Thread(() => GetProcess().Start());
            thread.Start();
        }

        #endregion

        #region Helper Methods

        private void ProcessExited(object sender, EventArgs e)
        {
            var process = (Process)sender;
            process.WaitForExit();

            _isProcessing = false;
        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if ((Convert.ToUInt32(OutputTargets) & Convert.ToUInt32(OutputTarget.Console)) != 0)
                Console.WriteLine(e.Data);

            if ((Convert.ToUInt32(OutputTargets) & Convert.ToUInt32(OutputTarget.LoggedOutput)) != 0)
                _output.AppendLine(e.Data);
        }

        private static void CopyStream(Stream input, Stream output)
        {
            var buffer = new byte[8 * 1024];
            int len;

            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, len);
            }
        }

        #endregion

        [Flags]
        public enum OutputTarget
        {
            None = 0,
            Console = 1,
            LoggedOutput = 2,
            All = Console | LoggedOutput
        }
    }
}
