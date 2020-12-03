package com.example.yuclientservice.Service;

import android.app.ActivityManager;
import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.graphics.PixelFormat;
import android.graphics.Point;
import android.net.Uri;
import android.os.Build;
import android.os.Handler;
import android.os.IBinder;
import android.preference.PreferenceManager;
import android.util.Log;
import android.view.Gravity;
import android.view.LayoutInflater;
import android.view.MotionEvent;
import android.view.View;
import android.view.WindowManager;
import android.widget.ArrayAdapter;
import android.widget.Spinner;
import android.widget.Toast;

import com.example.yuclientservice.MyApplication;
import com.example.yuclientservice.R;
import com.example.yuclientservice.Utils.ConstRef;

import java.util.ArrayList;
import java.util.Map;

public class FloatingClickService extends Service {
    private WindowManager mWindowManager;
    private View mFloatingView;
    private AutoClickService mAutoClickService = null;

    int mDuration = 32 * 1000;
    int mRepeatCount = 5;
    private Handler mHandler;
    private Runnable mRunable;

    private Spinner spinnerType;
    private Spinner spinnerButton;

    private ArrayList<Point> list1Line = new ArrayList<>();
    private Point mPointLike1 = null;
    private Point mPointDislike1 = null;
    private Point mPointSubcribe1 = null;
    private Point mPointSave1 = null;

    private SharedPreferences mPreferences = null;

    public FloatingClickService() {
    }

    @Override
    public IBinder onBind(Intent intent) {
        return null;
    }

    @Override
    public void onCreate() {
        super.onCreate();
        //Inflate the floating view layout we created
        mFloatingView = LayoutInflater.from(this).inflate(R.layout.layout_floating_widget, null);
        mAutoClickService = ((MyApplication)this.getApplication()).getClickService();
        //Add the view to the window.
        int overlayParam = (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O)?
            WindowManager.LayoutParams.TYPE_APPLICATION_OVERLAY : WindowManager.LayoutParams.TYPE_PHONE;

        final WindowManager.LayoutParams params = new WindowManager.LayoutParams(
                WindowManager.LayoutParams.WRAP_CONTENT,
                WindowManager.LayoutParams.WRAP_CONTENT,
                overlayParam,
                WindowManager.LayoutParams.FLAG_NOT_FOCUSABLE,
                PixelFormat.TRANSLUCENT);

        //Specify the view position
        params.gravity = Gravity.TOP | Gravity.LEFT;        //Initially view will be added to top-left corner
        params.x = 0;
        params.y = 100;

        //Add the view to the window
        mWindowManager = (WindowManager) getSystemService(WINDOW_SERVICE);
        mWindowManager.addView(mFloatingView, params);

        //Spinner
        this.spinnerType = (Spinner) mFloatingView.findViewById(R.id.spinnerType);
        this.spinnerButton = (Spinner) mFloatingView.findViewById(R.id.spinnerButton);

        String arrayType[] = {"1 line", "2 line","hasTag" };
        ArrayAdapter<String> typeAdapter = new ArrayAdapter<String>(this, android.R.layout.simple_spinner_item, arrayType);
        typeAdapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
        this.spinnerType.setAdapter(typeAdapter);

        String arrayButton[] = {ConstRef.LIKE1, ConstRef.DISLIKE1, ConstRef.SAVE1, ConstRef.SUBSCRIBE1 };
        ArrayAdapter<String> buttonAdapter = new ArrayAdapter<String>(this, android.R.layout.simple_spinner_item, arrayButton);
        typeAdapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
        this.spinnerButton.setAdapter(buttonAdapter);

        //The root element of the collapsed view layout
        final View collapsedView = mFloatingView.findViewById(R.id.collapse_view);
        final View expandedView = mFloatingView.findViewById(R.id.expanded_view);
        
        LoadSavePoint();

        //Drag and move floating view using user's touch action.
        mFloatingView.findViewById(R.id.root_container).setOnTouchListener(new View.OnTouchListener() {
            private int initialX;
            private int initialY;
            private float initialTouchX;
            private float initialTouchY;

            @Override
            public boolean onTouch(View v, MotionEvent event) {
                switch (event.getAction()) {
                    case MotionEvent.ACTION_DOWN:

                        //remember the initial position.
                        initialX = params.x;
                        initialY = params.y;

                        //get the touch location
                        initialTouchX = event.getRawX();
                        initialTouchY = event.getRawY();
                        return true;
                    case MotionEvent.ACTION_UP:
                        int Xdiff = (int) (event.getRawX() - initialTouchX);
                        int Ydiff = (int) (event.getRawY() - initialTouchY);
                        return true;
                    case MotionEvent.ACTION_MOVE:
                        //Calculate the X and Y coordinates of the view.
                        params.x = initialX + (int) (event.getRawX() - initialTouchX);
                        params.y = initialY + (int) (event.getRawY() - initialTouchY);

                        //Update the layout with new X & Y coordinate
                        mWindowManager.updateViewLayout(mFloatingView, params);
                        return true;
                }
                return false;
            }
        });

        //Run button
        mFloatingView.findViewById(R.id.btnPlay).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                stopRepeatVideo();
                //init duration, repeat times here
                //TODO: Duration and repeat time from server
                mHandler = new Handler();
                mDuration = 11 * 60 * 1000;
                mRepeatCount = 60;

                String url = "https://www.youtube.com/watch?v=y8M2M1kM6H8";
                playVideo(url);

                //Delay to click: after play 30 second
                long delay = mDuration/2 + 3000;
                clickLike(delay);
            }
        });

        mFloatingView.findViewById(R.id.btnCollapse).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                collapsedView.setVisibility(View.VISIBLE);
                expandedView.setVisibility(View.GONE);
            }
        });

        mFloatingView.findViewById(R.id.btnExpand).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                collapsedView.setVisibility(View.GONE);
                expandedView.setVisibility(View.VISIBLE);
            }
        });

        //Button record
        mFloatingView.findViewById(R.id.btnRecord).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                if(mPreferences == null) return;
                int location[] = new int[2];
                mFloatingView.getLocationOnScreen(location);
                Toast.makeText(FloatingClickService.this, "Record: " + location[0] +" -- " + location[1],Toast.LENGTH_SHORT).show();
                location[0] = (location[0] + mFloatingView.getRight()) / 2;

                switch (spinnerButton.getSelectedItem().toString()) {
                    case ConstRef.LIKE1:
                        mPointLike1 = new Point(location[0], location[1]);
                        mPreferences.edit().putString(ConstRef.LIKE1,location[0] + " " + location[1]).apply();
                        break;
                    case ConstRef.DISLIKE1:
                        mPointDislike1 = new Point(location[0], location[1]);
                        mPreferences.edit().putString(ConstRef.DISLIKE1,location[0] + " " + location[1]).apply();
                        break;
                    case ConstRef.SAVE1:
                        mPointSave1 = new Point(location[0], location[1]);
                        mPreferences.edit().putString(ConstRef.SAVE1,location[0] + " " + location[1]).apply();
                        break;
                    case ConstRef.SUBSCRIBE1:
                        mPointSubcribe1 = new Point(location[0], location[1]);
                        mPreferences.edit().putString(ConstRef.SUBSCRIBE1,location[0] + " " + location[1]).apply();
                        break;
                }
            }
        });

        //Button clear
        mFloatingView.findViewById(R.id.btnClear).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                mPreferences.edit().clear().apply();
            }
        });

        mFloatingView.findViewById(R.id.btnClose).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                stopSelf();
            }
        });
    }

    /*
    * Play video
    * */
    private void playVideo(String videoUrl){
        //Open youtube view
        Toast.makeText(FloatingClickService.this,"Play video" + "url", Toast.LENGTH_SHORT).show();
        Intent intent = new Intent(Intent.ACTION_VIEW);
        intent.setData(Uri.parse("https://www.youtube.com"));
        intent.setPackage("com.google.android.youtube");
        intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
        startActivity(intent);

        //need duration of Video + 10s, example 22 + 10
        //and time number to repeat video
        long time2Repeat = mDuration + 15 * 10 * 1000;

        mRunable = new Runnable() {
            @Override
            public void run() {
                try {
                    if(mRepeatCount-- > 0){
                        playVideo(videoUrl);
                        mHandler.postDelayed(mRunable, time2Repeat);
                    }else {
                        mHandler.removeCallbacks(mRunable);
                        Toast.makeText(FloatingClickService.this,"Finish repeat play", Toast.LENGTH_SHORT).show();
                    }
                }catch (Exception e){
                    e.printStackTrace();
                }
            }
        };
        mHandler.postDelayed(mRunable, 3000);
    }

    /*
    *
    */
    private void  clickLike(long delayClick)
    {
        //TODO: position click from server
        if(mPointLike1 != null){
            mHandler.postDelayed(new Runnable() {
                @Override
                public void run() {
                    Toast.makeText(FloatingClickService.this,"Liked" + mPointLike1.x+ "-" + mPointLike1.y, Toast.LENGTH_SHORT).show();
                    mAutoClickService.click(mPointLike1.x, mPointLike1.y);
                }
            },delayClick);
        }
    }

    /*
    * */
    private void clickSubcriber(long delayClick)
    {
        //TODO: position click from server
        if(mPointSubcribe1 != null){
            mHandler.postDelayed(new Runnable() {
                @Override
                public void run() {
                    Toast.makeText(FloatingClickService.this,"Liked" + mPointSubcribe1.x+ "-" + mPointSubcribe1.y, Toast.LENGTH_SHORT).show();
                    mAutoClickService.click(mPointSubcribe1.x, mPointSubcribe1.y);
                }
            },delayClick);
        }
    }

    /*
    *
    * */
    private void LoadSavePoint() {
        mPreferences = PreferenceManager.getDefaultSharedPreferences(this);
        //Load possition with one line title
        Map<String, ?> keys = mPreferences.getAll();
        if(keys.containsKey(ConstRef.LIKE1)){
            String like1 = keys.get(ConstRef.LIKE1).toString();
           mPointLike1 = passRefToPoint(like1);
            Log.d("Load Like1", like1);
        }
        if(keys.containsKey(ConstRef.DISLIKE1)){
            mPointLike1 = passRefToPoint(keys.get(ConstRef.DISLIKE1).toString());
        }
        if(keys.containsKey(ConstRef.SAVE1)){
            mPointLike1 = passRefToPoint(keys.get(ConstRef.SAVE1).toString());
        }
        if(keys.containsKey(ConstRef.SUBSCRIBE1)){
            mPointLike1 = passRefToPoint(keys.get(ConstRef.SUBSCRIBE1).toString());
        }
    }

    private Point passRefToPoint(String saveRef){
        String arr[] = saveRef.split(" ");
        try {
            Point point = new Point(Integer.parseInt(arr[0]), Integer.parseInt(arr[1]));
            return point;
        }catch (Exception ex){
            ex.printStackTrace();
        }
        return null;
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        super.onStartCommand(intent, flags, startId);
        return START_STICKY;
    }

    @Override
    public void onDestroy() {
        super.onDestroy();
        if (mFloatingView != null) mWindowManager.removeView(mFloatingView);

        if(mAutoClickService != null){
            mAutoClickService.stopSelf();
            mAutoClickService.disableSelf();
            mAutoClickService = null;
        }

    }

    /*
    * Stop looping play video
    */
    private void stopRepeatVideo(){
        if(mHandler !=null && mRunable!= null){
            mHandler.removeCallbacks(mRunable);
        }
    }

    /*
    * Kill running process
    * Can not work on unroot device
    * */
    public void KillApplication(String KillPackage)
    {
        ActivityManager activityManager = (ActivityManager)this.getSystemService(Context.ACTIVITY_SERVICE);

        Intent startMain = new Intent(Intent.ACTION_MAIN);
        startMain.addCategory(Intent.CATEGORY_HOME);
        startMain.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
        this.startActivity(startMain);

        Handler handler = new Handler();
        handler.postDelayed(new Runnable() {
            @Override
            public void run() {
                try {
                    //activityManager.killBackgroundProcesses(KillPackage);
                    Toast.makeText(getBaseContext(),"Process Killed : " + KillPackage  ,Toast.LENGTH_LONG).show();
                }catch (Exception e){
                    e.printStackTrace();
                }
            }
        }, 2000);
    }

    /**
     *
     * @return true if the floating view is collapsed.
     */
    private boolean isViewCollapsed() {
        return mFloatingView == null || mFloatingView.findViewById(R.id.collapse_view).getVisibility() == View.VISIBLE;
    }
}