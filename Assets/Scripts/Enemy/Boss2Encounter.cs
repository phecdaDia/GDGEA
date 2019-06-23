﻿using Camera;

namespace Enemy {
    using DG.Tweening;

    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class Boss2Encounter : PredefinedEncounter
    {
        public  string[] WinText;
        private int      access = 0;

        public float WalkDistance;
        public float WalkTime;

        public string SceneTo = "WinScene";

        public GameObject toDestroy;

        public FadeOut FadeOutPrefab;
		
        protected override void OnDone()
        {
            Destroy(this.toDestroy);
            
            if (GameManager.Instance.ShaderState <= ShaderState.Half)
            {
                GameManager.Instance.ShaderState = ShaderState.Half;
            }
			
            GameManager.Instance.isPaused = true;

            var camPos = GameManager.Instance.CameraController.transform.position.OnlyZ();
            GameManager.Instance.CameraController.transform.position = GameManager.Instance.OverWorldPlayer.transform
                                                                                  .position.ClearZ() + camPos;

            GameManager.Instance.CameraController.cameraMode = CameraMode.Static;
			
            this.NextBox();
			
            base.OnDone();
        }

        protected void NextBox()
        {
            if (this.access < this.WinText.Length)
            {
                GameManager.Instance.CameraController.SpawnTextBox(this.WinText[this.access++]).OnClosed(this.NextBox);
            }
            else
            {
                var player = GameManager.Instance.OverWorldPlayer;
                player.transform.DOMoveX(player.transform.position.x + this.WalkDistance, this.WalkTime)
                      .OnComplete(this.ChangeScene);
            }
        }

        protected void ChangeScene()
        {
            var fo = Instantiate(this.FadeOutPrefab);
            fo.OnDone = () => { SceneManager.LoadScene(this.SceneTo, LoadSceneMode.Single); };
        }
    }
}
