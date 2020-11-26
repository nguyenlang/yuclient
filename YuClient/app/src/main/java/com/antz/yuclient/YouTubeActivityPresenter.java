package com.antz.yuclient;

import android.os.AsyncTask;

import com.google.api.client.googleapis.extensions.android.gms.auth.UserRecoverableAuthIOException;
import com.google.api.services.youtube.YouTube;
import com.google.api.services.youtube.model.Comment;
import com.google.api.services.youtube.model.CommentSnippet;
import com.google.api.services.youtube.model.CommentThread;
import com.google.api.services.youtube.model.CommentThreadSnippet;
import com.google.api.services.youtube.model.ResourceId;
import com.google.api.services.youtube.model.Subscription;
import com.google.api.services.youtube.model.SubscriptionSnippet;

import java.io.IOException;

/**
 * Created by admin on 16-Oct-17.
 */

class   YouTubeActivityPresenter {
    private final YouTubeActivityView view;
    private final YouTubeSubscribeActivity activity;
    private final YouTube mService;

    YouTubeActivityPresenter(YouTubeActivityView view, YouTubeSubscribeActivity activity, YouTube service) {
        this.view = view;
        this.activity = activity;
        this.mService = service;
    }

    void subscribeToYouTubeChannel(String channelId) {

        new MakeRequestTask(mService, channelId).execute(); // creating AsyncTask for channel subscribe

    }

    void insertCommentToVideo(String videoId){
        new CommentRequestTask(mService, videoId).execute();
    }

    void rateVideo(String videoId, String rate)
    {
        new RateRequestTask(videoId, rate).execute();
    }
    /*
    * Comment to video
    * */
    private class CommentRequestTask extends AsyncTask<Object, Object, CommentThread>{
        //YouTube youTubeService = null;
        private String videoId;

        CommentRequestTask(YouTube service, String videoId){
            //this.youTubeService = service;
            this.videoId = videoId;
        }
        @Override
        protected CommentThread doInBackground(Object... params){
            CommentThread response = null;
            // Define the CommentThread object, which will be uploaded as the request body.
            CommentThread commentThread = new CommentThread();

            // Add the snippet object property to the CommentThread object.
            CommentThreadSnippet snippet = new CommentThreadSnippet();
            Comment topLevelComment = new Comment();
            CommentSnippet commentSnippet = new CommentSnippet();
            commentSnippet.setTextOriginal("It is very nice");
            topLevelComment.setSnippet(commentSnippet);
            snippet.setTopLevelComment(topLevelComment);
            snippet.setVideoId(videoId);
            commentThread.setSnippet(snippet);

            // Define and execute the API request
            try{
                YouTube.CommentThreads.Insert request = mService.commentThreads().insert("snippet",commentThread);
                response = request.execute();

            }catch (UserRecoverableAuthIOException e){
                e.printStackTrace();
                view.onGrantPermisson(e.getIntent());
            }catch (IOException ex)
            {
                ex.printStackTrace();
            }
            return response;
        }

        @Override
        protected void onPostExecute(CommentThread commentThread) {
            super.onPostExecute(commentThread);
            if(commentThread != null) {
               view.onCommentSuccess(commentThread.getSnippet().getVideoId());
            }else{
                view.onCallApiFail();
            }
        }
    }

    /*
    * Subcribe
    * */
    private class MakeRequestTask  extends AsyncTask<Object, Object, Subscription> {
        //private com.google.api.services.youtube.YouTube youTubeService = null;
        private String channelId;

        MakeRequestTask(YouTube service, String channelId) {
            this.channelId = channelId;
            //this.youTubeService = service;
        }

        @Override
        protected Subscription doInBackground(Object... params) {
            // code for channel subscribe
            Subscription response = null;

            // if you could not able to import the classes then check the dependency in build.gradle
            Subscription subscription = new Subscription();
            SubscriptionSnippet snippet = new SubscriptionSnippet();
            ResourceId resourceId = new ResourceId();
            resourceId.set("channelId", channelId);
            resourceId.set("kind", "youtube#channel");

            snippet.setResourceId(resourceId);
            subscription.setSnippet(snippet);

            YouTube.Subscriptions.Insert subscriptionsInsertRequest = null;
            try {
                subscriptionsInsertRequest = mService.subscriptions().insert("snippet", subscription);
                response = subscriptionsInsertRequest.execute();
            } catch (IOException e) {
               /* if you got error message below
                "message" : "Access Not Configured. YouTube Data API has not been used in project YOUR_PROJECT_ID before or it is disabled.
                then goto following link and enable the access
                https://console.developers.google.com/apis/api/youtube.googleapis.com/overview?project=YOUR_PROJECT_ID*/

                e.printStackTrace();
            }

            return response;
        }

        @Override
        protected void onPostExecute(Subscription subscription) {
            super.onPostExecute(subscription);
            if (subscription != null) {
                view.onSubscriptionSuccess(subscription.getSnippet().getTitle());
            } else {
                view.onCallApiFail();
            }
        }
    }

    /*
    * Rate video
    * */
    private class RateRequestTask  extends AsyncTask<Object, Object, String> {

        private String videoId;
        private String rate;

        RateRequestTask(String videoId, String rate) {
            this.videoId = videoId;
            this.rate = rate;
            //this.youTubeService = service;
        }
        @Override
        protected String doInBackground(Object... params) {
            String response = null;
            try{
                YouTube.Videos.Rate request = mService.videos().rate(videoId, rate);
                request.execute();
                response = "Success";
            }catch (Exception ex){
                ex.printStackTrace();
            }
            return  response;
        }

        @Override
        protected void onPostExecute(String response) {
            super.onPostExecute(response);
            if (response != null) {
                view.onRateSuccess();
            } else {
                view.onCallApiFail();
            }
        }
    }
}
