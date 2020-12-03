package com.example.yuclientservice.Service;

import android.accessibilityservice.AccessibilityService;
import android.accessibilityservice.GestureDescription;
import android.content.Intent;
import android.graphics.Path;
import android.os.Build;
import android.util.Log;
import android.view.accessibility.AccessibilityEvent;
import android.widget.Toast;

import com.example.yuclientservice.Activity.MainActivity;
import com.example.yuclientservice.MyApplication;
import com.example.yuclientservice.Utils.Event;

import java.util.ArrayList;
import java.util.List;

public class AutoClickService extends AccessibilityService {
    List<Event> events = new ArrayList<Event>();
    String TAG = "CLICK_SERVICE";
    public AutoClickService() {
    }

    @Override
    public void onAccessibilityEvent(AccessibilityEvent event) {
        //TODO:
    }

    @Override
    public void onInterrupt() {
        //TODO
    }

    @Override
    protected void onServiceConnected() {
        super.onServiceConnected();

        Toast.makeText(this, "Connect accessibility service", Toast.LENGTH_LONG).show();
        ((MyApplication)this.getApplication()).setClickService(this);
        Intent intent = new Intent(this, MainActivity.class);
        intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
        startActivity(intent);
    }

    public void click(int x, int y){
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.N) return;
        Path path =new Path();
        path.moveTo(x, y);
        GestureDescription.Builder builder = new GestureDescription.Builder();
        GestureDescription gestureDescription = builder.addStroke(new GestureDescription.StrokeDescription(path,10,10)).build();
        dispatchGesture(gestureDescription, null, null);
    }
}