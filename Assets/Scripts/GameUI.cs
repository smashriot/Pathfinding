// -------------------------------------------------------------------------------------------------
//  GameUI.cs
//  Created by Jesse Ozog (code@smashriot.com) on 2016/04/18
//  Copyright 2016 SmashRiot, LLC. All rights reserved.
// -------------------------------------------------------------------------------------------------
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour {

    public GameMain gameMain;
    public Text mapName;
    public Button m1Button;
    public Button m2Button;
    public Button m3Button;
    public Button m4Button;
    public Button m5Button;
    public Button pfVisButton;
    public Button pauseButton;
    public Button stemButton;
    public InputField maxChecksInputField;
    public InputField borderCostInputField;
    public InputField diagCostInputField;
    public InputField adjCostInputField;

    // -------------------------------------------------------------------------------------------------
    // -------------------------------------------------------------------------------------------------
    public void Start(){

        // buttons
        this.m1Button.onClick.AddListener(delegate { m1ButtonClicked(); });
        this.m2Button.onClick.AddListener(delegate { m2ButtonClicked(); });
        this.m3Button.onClick.AddListener(delegate { m3ButtonClicked(); });
        this.m4Button.onClick.AddListener(delegate { m4ButtonClicked(); });
        this.m5Button.onClick.AddListener(delegate { m5ButtonClicked(); });
        this.pfVisButton.onClick.AddListener(delegate { togglePathfindingClicked(); });
        this.pauseButton.onClick.AddListener(delegate { togglePauseClicked(); });
        this.stemButton.onClick.AddListener(delegate { stemButtonClicked(); });

        // fields
        this.maxChecksInputField.onEndEdit.AddListener(delegate { maxChecksChanged(); });
        this.borderCostInputField.onEndEdit.AddListener(delegate { borderCostChanged(); });
        this.diagCostInputField.onEndEdit.AddListener(delegate { diagCostChanged(); });
        this.adjCostInputField.onEndEdit.AddListener(delegate { adjCostChanged(); });

        // get current field value (pref, default value)
        this.maxChecksInputField.text = PlayerPrefs.GetInt(Constants.PREFERENCE_PATHFINDING_MAX_CHECKS, Constants.PATHFINDING_CONFIG_DEFAULT_MAX_CHECKS).ToString();
        this.borderCostInputField.text = PlayerPrefs.GetInt(Constants.PREFERENCE_PATHFINDING_BORDER_COST, Constants.PATHFINDING_CONFIG_DEFAULT_BORDER_COST).ToString();
        this.diagCostInputField.text = PlayerPrefs.GetInt(Constants.PREFERENCE_PATHFINDING_DIAG_COST, Constants.PATHFINDING_CONFIG_DEFAULT_DIAG_COST).ToString();
        this.adjCostInputField.text = PlayerPrefs.GetInt(Constants.PREFERENCE_PATHFINDING_ADJ_COST, Constants.PATHFINDING_CONFIG_DEFAULT_ADJ_COST).ToString();
    }

    // ------------------------------------------------------------------------
    // ------------------------------------------------------------------------
    public void reset(){
        Text buttonText = this.pfVisButton.GetComponentInChildren<Text>();
        buttonText.text = "Hide";

        Time.timeScale = 1.0f;
        buttonText = this.pauseButton.GetComponentInChildren<Text>();
        buttonText.text = "Pause";
    }

    // ------------------------------------------------------------------------
    // ------------------------------------------------------------------------
    public void m1ButtonClicked(){
        gameMain.loadMap(1);
        this.mapName.text = "Map 1";
    }

    // ------------------------------------------------------------------------
    // ------------------------------------------------------------------------
    public void m2ButtonClicked(){
        gameMain.loadMap(2);
        this.mapName.text = "Map 2";
    }

    // ------------------------------------------------------------------------
    // ------------------------------------------------------------------------
    public void m3ButtonClicked(){
        gameMain.loadMap(3);
        this.mapName.text = "Map 3";
    }

    // ------------------------------------------------------------------------
    // ------------------------------------------------------------------------
    public void m4ButtonClicked(){
        gameMain.loadMap(4);
        this.mapName.text = "Map 4";
    }

    // ------------------------------------------------------------------------
    // ------------------------------------------------------------------------
    public void m5ButtonClicked(){
        gameMain.loadMap(5);
        this.mapName.text = "Map 5";
    }

    // ------------------------------------------------------------------------
    // ------------------------------------------------------------------------
    public void stemButtonClicked(){
        Application.OpenURL("http://SmashRiot.com/stem/pf");
    }

    // ------------------------------------------------------------------------
    // toggle active state. since TILEMAP_DEBUG_GO is created/destroyed per cat,
    // need to do a super expensive FindObjectsOfTypeAll in case it was set inactive
    // which won't be found by a simple GameObject.Find(Constants.TILEMAP_DEBUG_GO);
    // and foreach is evil since creates lots of garbage
    // ------------------------------------------------------------------------
    public void togglePathfindingClicked(){

        GameObject[] foundGameObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
        if (foundGameObjects.Length > 0){
            for (int i=0; i<foundGameObjects.Length; i++){
                GameObject currentGO = foundGameObjects[i];
                if (currentGO.name == Constants.TILEMAP_DEBUG_GO){
                    // toggle active state
                    if (!currentGO.activeInHierarchy){
                        currentGO.SetActive(true);
                        Text buttonText = this.pfVisButton.GetComponentInChildren<Text>();
                        buttonText.text = "Hide";
                    }
                    else {
                        currentGO.SetActive(false);
                        Text buttonText = this.pfVisButton.GetComponentInChildren<Text>();
                        buttonText.text = "Show";
                    }
                }
            }
        }
    }

    // ------------------------------------------------------------------------
    // pauses object root GO
    // ------------------------------------------------------------------------
    public void togglePauseClicked(){

        // if less than 1.0 (paused), then resume
        if (Time.timeScale < 1.0f){
            Time.timeScale = 1.0f;
            Text buttonText = this.pauseButton.GetComponentInChildren<Text>();
            buttonText.text = "Pause";
        }
        // otherwise, pause
        else {
            Time.timeScale = 0.0f;
            Text buttonText = this.pauseButton.GetComponentInChildren<Text>();
            buttonText.text = "Resume";
        }
    }

    // ------------------------------------------------------------------------
    // reads, validates, and saves field.
    // ------------------------------------------------------------------------
    private void validateAndSaveField(InputField inputField, string preference, int max){

        int fieldValue = int.Parse(inputField.text);
        if (fieldValue > max){
            fieldValue = max;
            inputField.text = fieldValue.ToString();
        }
          PlayerPrefs.SetInt(preference, fieldValue);
        PlayerPrefs.Save();
    }

    // ------------------------------------------------------------------------
    // ------------------------------------------------------------------------
    public void maxChecksChanged(){

        this.validateAndSaveField(this.maxChecksInputField, Constants.PREFERENCE_PATHFINDING_MAX_CHECKS, Constants.PATHFINDING_CONFIG_FINAL_MAX_CHECKS);
     }

    // ------------------------------------------------------------------------
    // ------------------------------------------------------------------------
    public void borderCostChanged(){

       this.validateAndSaveField(this.borderCostInputField, Constants.PREFERENCE_PATHFINDING_BORDER_COST, 100);
    }

    // ------------------------------------------------------------------------
    // ------------------------------------------------------------------------
    public void diagCostChanged(){

       this.validateAndSaveField(this.diagCostInputField, Constants.PREFERENCE_PATHFINDING_DIAG_COST, 100);
    }

    // ------------------------------------------------------------------------
    // ------------------------------------------------------------------------
    public void adjCostChanged(){

        this.validateAndSaveField(this.adjCostInputField, Constants.PREFERENCE_PATHFINDING_ADJ_COST, 100);
    }
}
