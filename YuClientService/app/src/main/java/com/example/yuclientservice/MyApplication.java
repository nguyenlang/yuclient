package com.example.yuclientservice;

import android.app.Application;

import com.example.yuclientservice.Service.AutoClickService;

public class MyApplication extends Application {
    private AutoClickService autoClickService = null;

    public AutoClickService getClickService() {
        return autoClickService;
    }

    public void setClickService(AutoClickService service) {
        this.autoClickService = service;
    }

}
