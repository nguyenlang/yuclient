package com.antz.yuclient;

import android.content.Intent;

/**
 * Created by admin on 16-Oct-17.
 */

public interface YouTubeActivityView {
    void onSubscriptionSuccess(String title);

    void onCallApiFail();

    void onCommentSuccess(String title);

    void onRateSuccess();

    void onGrantPermisson(Intent i);
}
