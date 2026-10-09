package com.androiddesktop.diagnostic;

import android.app.Activity;
import android.content.res.Configuration;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Paint;
import android.media.AudioManager;
import android.media.ToneGenerator;
import android.os.Bundle;
import android.os.SystemClock;
import android.util.Log;
import android.view.Choreographer;
import android.view.MotionEvent;
import android.view.KeyEvent;
import android.view.View;
import android.view.WindowManager;
import org.json.JSONArray;
import org.json.JSONObject;
import java.util.UUID;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

/** Hardware measurement target; timestamps are Android uptime, not host performance.now. */
public final class DiagnosticActivity extends Activity {
    private final String run = UUID.randomUUID().toString();
    private long sequence, touches, savedTouches, frame, lastPulse;
    private float lastX, lastY;
    private int pointerCount, color = Color.rgb(17,24,39);
    private ToneGenerator tone;
    private DiagnosticView view;
    private boolean resumed;
    private final ExecutorService saves = Executors.newSingleThreadExecutor();
    private final Choreographer.FrameCallback frames = new Choreographer.FrameCallback() {
        @Override public void doFrame(long nanos) {
            if (!resumed) return;
            frame++;
            long now = SystemClock.uptimeMillis();
            if (now-lastPulse >= 1000) {
                lastPulse = now;
                tone.startTone(ToneGenerator.TONE_PROP_BEEP, 80);
                log("pulse", "{\"frameNanos\":" + nanos + ",\"requestedUptimeMs\":" + now + "}");
            }
            view.invalidate();
            Choreographer.getInstance().postFrameCallback(this);
        }
    };
    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON | WindowManager.LayoutParams.FLAG_FULLSCREEN);
        savedTouches = getPreferences(MODE_PRIVATE).getLong("touches",0); touches = savedTouches;
        tone = new ToneGenerator(AudioManager.STREAM_MUSIC,60);
        view = new DiagnosticView(); setContentView(view); view.requestFocus();
        log("created", "{\"savedTouches\":" + savedTouches + "}");
    }
    @Override public void onResume() { super.onResume(); resumed=true; Choreographer.getInstance().postFrameCallback(frames); }
    @Override public void onPause() { resumed=false; Choreographer.getInstance().removeFrameCallback(frames); tone.stopTone(); super.onPause(); }
    @Override public void onDestroy() { tone.release(); saves.shutdown(); super.onDestroy(); }
    @Override public void onConfigurationChanged(Configuration configuration) { super.onConfigurationChanged(configuration); log("configuration", "{\"orientation\":"+configuration.orientation+"}"); }
    @Override public boolean dispatchKeyEvent(KeyEvent event) {
        log("key", "{\"action\":"+event.getAction()+",\"keyCode\":"+event.getKeyCode()+",\"repeatCount\":"+event.getRepeatCount()+",\"eventTimeMs\":"+event.getEventTime()+",\"downTimeMs\":"+event.getDownTime()+"}");
        return super.dispatchKeyEvent(event);
    }
    private void log(String kind, String payload) {
        Log.i("Phase0Diagnostic", "{\"run\":\""+run+"\",\"sequence\":"+(++sequence)+",\"kind\":\""+kind+"\",\"receivedUptimeMs\":"+SystemClock.uptimeMillis()+",\"data\":"+payload+"}");
    }
    private final class DiagnosticView extends View {
        final Paint paint = new Paint(Paint.ANTI_ALIAS_FLAG);
        DiagnosticView() { super(DiagnosticActivity.this); setFocusable(true); setFocusableInTouchMode(true); }
        @Override public boolean onTouchEvent(MotionEvent event) {
            try {
                JSONObject result = new JSONObject(); JSONArray points = new JSONArray();
                result.put("actionMasked",event.getActionMasked()); result.put("actionIndex",event.getActionIndex());
                result.put("eventTimeMs",event.getEventTime()); result.put("downTimeMs",event.getDownTime());
                result.put("width",getWidth()); result.put("height",getHeight());
                result.put("rotation",getDisplay().getRotation());
                for (int i=0;i<event.getPointerCount();i++) {
                    JSONObject point = new JSONObject(); point.put("id",event.getPointerId(i));
                    point.put("x",event.getX(i)); point.put("y",event.getY(i)); point.put("pressure",event.getPressure(i)); points.put(point);
                }
                result.put("pointers",points); result.put("historySize",event.getHistorySize());
                JSONArray history = new JSONArray();
                for (int sample=0; sample<event.getHistorySize();sample++) {
                    JSONObject historical = new JSONObject(); historical.put("eventTimeMs",event.getHistoricalEventTime(sample));
                    JSONArray hp = new JSONArray();
                    for (int i=0;i<event.getPointerCount();i++) { JSONObject p = new JSONObject(); p.put("id",event.getPointerId(i)); p.put("x",event.getHistoricalX(i,sample)); p.put("y",event.getHistoricalY(i,sample)); hp.put(p); }
                    historical.put("pointers",hp); history.put(historical);
                }
                result.put("history",history); log("touch",result.toString());
            } catch (Exception failure) { Log.e("Phase0Diagnostic","Touch log failure",failure); }
            int action=event.getActionMasked();
            lastX=event.getX(event.getActionIndex()); lastY=event.getY(event.getActionIndex());
            pointerCount=event.getPointerCount();
            if (action==MotionEvent.ACTION_UP || action==MotionEvent.ACTION_CANCEL) pointerCount=0;
            if (action==MotionEvent.ACTION_POINTER_UP) pointerCount--;
            if (action==MotionEvent.ACTION_DOWN) {
                touches++; final long value=touches;
                saves.execute(() -> {
                    boolean committed=getPreferences(MODE_PRIVATE).edit().putLong("touches",value).commit();
                    Log.i("Phase0Diagnostic","{\"kind\":\"save\",\"touches\":"+value+",\"committed\":"+committed+"}");
                });
                color=Color.rgb((int)(touches*47%160)+40,(int)(touches*83%160)+40,(int)(touches*29%160)+40);
            }
            invalidate(); return true;
        }
        @Override protected void onSizeChanged(int w,int h,int ow,int oh) { log("geometry","{\"width\":"+w+",\"height\":"+h+",\"rotation\":"+getDisplay().getRotation()+"}"); }
        @Override protected void onDraw(Canvas c) {
            c.drawColor(color); paint.setColor(Color.WHITE); paint.setTextSize(32);
            c.drawText("Phase 0 — touch anywhere",24,56,paint);
            c.drawText("Frame: "+frame+" / active pointers: "+pointerCount,24,102,paint);
            c.drawText("Touches: "+touches+" / saved at launch: "+savedTouches,24,148,paint);
            c.drawText(getWidth()+" x "+getHeight()+" / rotation: "+getDisplay().getRotation(),24,194,paint);
            c.drawText("Beep + square each second; compare sync externally",24,240,paint);
            paint.setColor(SystemClock.uptimeMillis()-lastPulse<80?Color.WHITE:Color.BLACK);
            c.drawRect(getWidth()-100,0,getWidth(),100,paint);
            paint.setColor(Color.CYAN); c.drawCircle(lastX,lastY,25,paint);
            paint.setColor(Color.YELLOW); c.drawLine(0,getHeight()/2f,getWidth(),getHeight()/2f,paint); c.drawLine(getWidth()/2f,0,getWidth()/2f,getHeight(),paint);
        }
    }
}
