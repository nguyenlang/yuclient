package com.example.yuclientservice.Activity;

import androidx.annotation.Nullable;
import androidx.appcompat.app.AppCompatActivity;

import android.accessibilityservice.AccessibilityService;
import android.accessibilityservice.AccessibilityServiceInfo;
import android.annotation.TargetApi;
import android.app.ActivityManager;
import android.content.Context;
import android.content.Intent;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.provider.Settings;
import android.util.Log;
import android.view.View;
import android.view.accessibility.AccessibilityManager;
import android.widget.Toast;

import com.example.yuclientservice.MyApplication;
import com.example.yuclientservice.R;
import com.example.yuclientservice.Service.AutoClickService;
import com.example.yuclientservice.Service.FloatingClickService;

import java.util.List;
import java.util.Set;

public class MainActivity extends AppCompatActivity {

    private static final int CODE_DRAW_OVER_OTHER_APP_PERMISSION = 2084;
    private static final int PERMISSION_CODE = 110;
    private Intent mServiceIntent = null;
    private String TAG ="MAIN";
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        //Check if the application has draw over other apps permission or not?
        //This permission is by default available for API<23. But for API > 23
        //you have to ask for the permission in runtime.

        findViewById(R.id.btnStartService).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                if(Build.VERSION.SDK_INT < Build.VERSION_CODES.N
                        || Settings.canDrawOverlays(MainActivity.this)){
                    Log.d(TAG, "onClick: start service on android lower N");
                    mServiceIntent = new Intent(MainActivity.this, FloatingClickService.class);
                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                        startForegroundService(mServiceIntent);
                    } else {
                        startService(mServiceIntent);
                    }

                    backToScreen();
                    finish();
                    //moveTaskToBack(true);
                }else {
                    askPermission();
                    Toast.makeText(MainActivity.this, "You need System Alert Window Permission to do this", Toast.LENGTH_LONG).show();
                }
            }
        });
    }

    /*
    *Check and start accessibility service
    */
    private Boolean checkAccessibility(){
        String serviceId = getString(R.string.accessibility_service_id);
        AccessibilityManager manager = (AccessibilityManager)getSystemService(Context.ACCESSIBILITY_SERVICE);
        List<AccessibilityServiceInfo> listService = manager.getEnabledAccessibilityServiceList(AccessibilityServiceInfo.FEEDBACK_GENERIC);
        for(AccessibilityServiceInfo i : listService ){
            String id = i.getId();
            if(serviceId.equals(id)){
                return true;
            }
        }
        return false;
    }

    /*
    * Ask permission
    * */
    private void askPermission() {
        Intent intent = new Intent(Settings.ACTION_MANAGE_OVERLAY_PERMISSION,
                Uri.parse("package:" + getPackageName()));
        startActivityForResult(intent, PERMISSION_CODE);
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        if (requestCode == CODE_DRAW_OVER_OTHER_APP_PERMISSION) {

            //Check if the permission is granted or not.
            if (resultCode == RESULT_OK) {
                if(mServiceIntent != null){
                    mServiceIntent = new Intent(this, FloatingClickService.class);
                }
                startActivity(mServiceIntent);
                backToScreen();
                finish();
                //moveTaskToBack(true);
            } else { //Permission is not available
                Toast.makeText(this,
                        "Draw over other app permission not available. Closing the application",
                        Toast.LENGTH_SHORT).show();
                finish();
            }
        } else {
            super.onActivityResult(requestCode, resultCode, data);
        }
    }

    @Override
    protected void onResume() {
        super.onResume();
        boolean hasPermission = checkAccessibility();
        Log.d("Main", "onResume: has access permission" + hasPermission);
        if(!hasPermission){
            startActivity(new Intent(Settings.ACTION_ACCESSIBILITY_SETTINGS));
        }
        if(Build.VERSION.SDK_INT >= Build.VERSION_CODES.M
        && !Settings.canDrawOverlays(this)){
            askPermission();
        }
    }

    @Override
    protected void onStart() {
        super.onStart();
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
//        if(mServiceIntent != null)
//        {
//            stopService(mServiceIntent);
//            Log.d(TAG, "onDestroy: stop floating service");
//        }
//        AutoClickService autoClickService = ((MyApplication)this.getApplication()).getClickService();
//        if(autoClickService != null){
//            autoClickService.stopSelf();
//            autoClickService.disableSelf();
//            autoClickService = null;
//        }

    }

    public void backToScreen()
    {
        Intent startMain = new Intent(Intent.ACTION_MAIN);
        startMain.addCategory(Intent.CATEGORY_HOME);
        startMain.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
        this.startActivity(startMain);
    }
}