package com.example.yuclientservice.Utils;


import android.content.Context;

public final class Extentions {
    public final String TAG = "ClickService";

    public static int dp2px(Context context, float value)
    {
        float scale = context.getResources().getDisplayMetrics().density;
        return Math.round(value * scale + 0.5f);
    }
}

