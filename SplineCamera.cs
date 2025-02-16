﻿using System;
using System.Collections.Generic;
using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GTAV_ScriptCamTool
{
    public class SplineCamera
    {
        UIContainer msgBox = new UIContainer(new Point(1100, 0), new Size(150, 34), Color.FromArgb(140, 0, 0, 0));
        string msgText;
        float msgScale;
        Timer msgTimer = new Timer(1000);
        private bool _usePlayerView;
        private Timer _renderSceneTimer;
        private Camera _mainCamera;
        private List<Tuple<Vector3, Vector3>> _nodes;
        private Timer _replayTimer;
        private Vector3 _startPos, _previousPos;
        public Camera MainCamera { get { return _mainCamera; } }

        int currentSegment = 0;
        private int _speed;
        public bool InterpToPlayer { get; set; }

        public bool UsePlayerView
        {
            get
            {
                return _usePlayerView;
            }
            set
            {
                if (value)
                {
                    _startPos = Game.Player.Character.Position;
                    Game.Player.Character.IsInvincible = true;
                    Game.Player.Character.IsVisible = false;
                }

                else
                {
                    if (_startPos != null)
                    Game.Player.Character.Position = _startPos;
                    Game.Player.Character.IsInvincible = false;
                    Game.Player.Character.IsVisible = true;
                }

                this._usePlayerView = value;
            }
        }

        public int Speed { set {
            this._speed = value;
            Function.Call(Hash.SET_CAM_SPLINE_DURATION, _mainCamera.Handle,  100 / value * 1000 ); 
            } }

        public List<Tuple<Vector3, Vector3>> Nodes {  get { return _nodes; } set { _nodes = value; } }

        public SplineCamera()
        {
            this._mainCamera = new Camera(Function.Call<int>(Hash.CREATE_CAM, "DEFAULT_SPLINE_CAMERA", 0));
            this._nodes = new List<Tuple<Vector3, Vector3>>();
            this._replayTimer = new Timer(1100);
            this._renderSceneTimer = new Timer(5000);
            this._renderSceneTimer.Start();
            this.currentSegment = 0;
            this._speed = 1;
        }

        public void AddNode(Vector3 position, Vector3 rotation, int duration)
        {
            this._nodes.Add(new Tuple<Vector3, Vector3>(position, rotation));
            //Maybe move next line to render time to do subsets of _nodes
            // Function.Call(Hash.ADD_CAM_SPLINE_NODE, _mainCamera.Handle, position.X, position.Y, position.Z, rotation.X, rotation.Y, rotation.Z, duration, 3, 2);
        }


        public void addNextSegments() {
            Tuple<Vector3, Vector3> item;
            for (int i = this.currentSegment; i < _nodes.Count; i++) {
                item = _nodes[i];
                Function.Call(Hash.ADD_CAM_SPLINE_NODE, _mainCamera.Handle, item.Item1[0], item.Item1[1], item.Item1[2], item.Item2[0], item.Item2[1], item.Item2[2], 4000, 3, 0);
                
                msgText = string.Format("~e~Added node index: {0}", i.ToString());
                UI.ShowSubtitle("Added node: " + i.ToString());
            }
        }

        public void updateSegments(int segment) {
            this.currentSegment = segment;
            Function.Call(Hash.DESTROY_ALL_CAMS, true);
            this._mainCamera = new Camera(Function.Call<int>(Hash.CREATE_CAM, "DEFAULT_SPLINE_CAMERA", 0));
            this.Speed = this._speed;
            addNextSegments();
            Function.Call(Hash.SET_CAM_SPLINE_PHASE, _mainCamera.Handle, 0f);
        }
        public void EnterCameraView(Vector3 position)
        {
            UI.ShowSubtitle("EnterCameraView()");
            // Function.Call(Hash.DO_SCREEN_FADE_OUT, 1200);
            Script.Wait(1100);
            updateSegments(0);
            MainCamera.Position = position;
            MainCamera.IsActive = true;
            World.RenderingCamera = MainCamera;
            Script.Wait(100);
            // Function.Call(Hash.DO_SCREEN_FADE_IN, 800);
        }

        public void ExitCameraView()
        {
            Function.Call(Hash.DO_SCREEN_FADE_OUT, 1200);
            Script.Wait(1100);
            MainCamera.IsActive = false;
            if (UsePlayerView)
                UsePlayerView = false;
            World.RenderingCamera = null;
            Script.Wait(100);
            Function.Call(Hash.CLEAR_FOCUS);
            Function.Call(Hash.DO_SCREEN_FADE_IN, 800);
        }

        public void Update()
        {
            if (MainCamera.IsActive)
            {
                //Print debug info here
                if ((Function.Call<int>(Hash.GET_CAM_SPLINE_NODE_INDEX, _mainCamera.Handle) == 0) || (Function.Call<int>(Hash.GET_CAM_SPLINE_NODE_INDEX, _mainCamera.Handle) > 16)) {
                    msgScale = 0.3f;
                    msgBox.Items.Clear();
                    msgBox.Items.Add(new UIText(msgText, new Point(1121, 2), msgScale, Color.White));
                    msgBox.Draw();
                    msgBox.Items[0].Draw();
                }
                else {
                    msgText = string.Format("~e~Node Max Index: {0}, Current node index: {1}, Current segment: {2}", _nodes.Count - 1, Function.Call<int>(Hash.GET_CAM_SPLINE_NODE_INDEX, _mainCamera.Handle).ToString(), this.currentSegment);
                    msgScale = 0.3f;
                    msgBox.Items.Clear();
                    msgBox.Items.Add(new UIText(msgText, new Point(1121, 2), msgScale, Color.White));
                    msgBox.Draw();
                    msgBox.Items[0].Draw();
                }
                //refresh render scene
                if (_renderSceneTimer.Enabled && Game.GameTime > _renderSceneTimer.Waiter)
                {
                    Function.Call(Hash._0x0923DBF87DFF735E, _mainCamera.Position.X, _mainCamera.Position.Y, _mainCamera.Position.Z);
                    _renderSceneTimer.Reset();
                }




                Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);
                Function.Call(Hash.HIDE_HUD_COMPONENT_THIS_FRAME, 18);
        
                _previousPos = _mainCamera.Position;


                if (Function.Call<int>(Hash.GET_CAM_SPLINE_NODE_INDEX, _mainCamera.Handle) > 15) {
                        UI.ShowSubtitle("Reached index 16 adding new nodes");
                        Script.Wait(1100);
                        this.currentSegment += 16;
                        updateSegments(this.currentSegment);
                        MainCamera.IsActive = true;
                        World.RenderingCamera = MainCamera;
                         _replayTimer.Enabled = false;
                }

                //reset camera to start
                if (_replayTimer.Enabled && Game.GameTime > _replayTimer.Waiter)
                {
                    // if(currentSegment + 17 < _nodes.Count) {
                    //     //Next segment
                    //     UI.ShowSubtitle("Reached end adding new nodes");
                    //     Script.Wait(1100);
                    //     this.currentSegment += 17;
                    //     updateSegments(this.currentSegment);
                    //     MainCamera.IsActive = true;
                    //     World.RenderingCamera = MainCamera;
                    // }
                    // else {
                    //     UI.ShowSubtitle("Resetting Camera");
                    //     Script.Wait(1100);
                    //     this.currentSegment = 0;
                    //     updateSegments(this.currentSegment);
                    //     MainCamera.IsActive = true;
                    //     World.RenderingCamera = MainCamera;
                    //     Function.Call(Hash.SET_CAM_SPLINE_PHASE, _mainCamera.Handle, 0f);
                    // }
                    Function.Call(Hash.SET_CAM_SPLINE_PHASE, _mainCamera.Handle, 0f);
                    _replayTimer.Enabled = false;
                }

                //If not interpolating, replay. this is what ACTUALLY triggers replay
                if (!_mainCamera.IsInterpolating)
                {
                    if (Function.Call<float>(Hash.GET_CAM_SPLINE_PHASE, _mainCamera.Handle) > 0.001f)
                    {
                        if (InterpToPlayer)
                        {
                            Function.Call(Hash.RENDER_SCRIPT_CAMS, 0, 1, 3000, 1, 1, 1);

                            Function.Call(Hash.CLEAR_FOCUS);
                            MainCamera.IsActive = false;
                        }
                    }

                    if (!_replayTimer.Enabled)
                        _replayTimer.Start();
                }

                else
                {
                    if (UsePlayerView)
                        Game.Player.Character.Position = MainCamera.Position;

                    else
                    {
                        //render local scene
                        var lastPos = Vector3.Subtract(_mainCamera.Position, _previousPos);
                        Function.Call(Hash._SET_FOCUS_AREA, _mainCamera.Position.X, _mainCamera.Position.Y, _mainCamera.Position.Z, lastPos.X, lastPos.Y, lastPos.Z);
                    }
                }
            }
        }
    }
}

