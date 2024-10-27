using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class CanvasController : MonoBehaviour
{
    public Toggle randomColorToggle;
    public Slider redSlider;
    public Slider greenSlider;
    public Slider blueSlider;
    public TextMeshProUGUI redLabel;
    public TextMeshProUGUI greenLabel;
    public TextMeshProUGUI blueLabel;
    public Image colorDisplay;
    public Button saveButton;
    public Button restartButton;
    public Slider updatesPerFrame;
    public Slider widthSlider;
    public Slider heightSlider;
    public TextMeshProUGUI widthSliderLabel;
    public TextMeshProUGUI heightSliderLabel;
    public Slider pixelChangeScaleSlider;

    public Slider speciesSlider;
    public TextMeshProUGUI speciesSliderLabel;
    public Toggle autoRestartToggle;
    public Toggle savePixelInfoToggle;
    public TMP_Dropdown secondaryView;
    public TMP_Dropdown updateRule;

    public Camera secondaryCamera;
    private GeneSheepManager geneSheepManager;
    private DrawTexture drawTextureScript;

    // Start is called before the first frame update
    void Start()
    {
        randomColorToggle.onValueChanged.AddListener(delegate { OnColorsChanged(); });
        redSlider.onValueChanged.AddListener(delegate { OnColorsChanged(); });
        greenSlider.onValueChanged.AddListener(delegate { OnColorsChanged(); });
        blueSlider.onValueChanged.AddListener(delegate { OnColorsChanged(); });

        updatesPerFrame.onValueChanged.AddListener(delegate { OnUpdatesPerFrameChanged(); });

        secondaryView.onValueChanged.AddListener(delegate { SecondaryViewChanged(); });

        saveButton.onClick.AddListener(delegate { OnSaveButtonClicked(); });

        restartButton.onClick.AddListener(delegate { OnRestartButtonClicked(); });

        autoRestartToggle.onValueChanged.AddListener(delegate { OnAutoRestartChanged(); });

        savePixelInfoToggle.onValueChanged.AddListener(delegate { OnSavePixelInfoChanged(); });
        
        geneSheepManager = Camera.main.GetComponent<GeneSheepManager>();
        drawTextureScript = secondaryCamera.GetComponent<DrawTexture>();

        widthSlider.onValueChanged.AddListener(delegate { OnDimensionsChanged(); });
        heightSlider.onValueChanged.AddListener(delegate { OnDimensionsChanged(); });

        speciesSlider.onValueChanged.AddListener(delegate { OnSpeciesChanged(); });

        pixelChangeScaleSlider.onValueChanged.AddListener(delegate { OnPixelChangeScaleChanged(); });

        updateRule.onValueChanged.AddListener(delegate { OnUpdateRuleChanged(); });
    }

    // Update is called once per frame
    void Update()
    {
    }

    void SecondaryViewChanged() {
        drawTextureScript.mode = secondaryView.value;
    }

    void OnColorsChanged() {
        float r = redSlider.value / 255f;
        float g = greenSlider.value / 255f;
        float b = blueSlider.value / 255f;
        redLabel.text = redSlider.value.ToString();
        redLabel.color = new Color(r, 0, 0, 1);
        greenLabel.text = greenSlider.value.ToString();
        greenLabel.color = new Color(0, g, 0, 1);
        blueLabel.text = blueSlider.value.ToString();
        blueLabel.color = new Color(0, 0, b, 1);

        Color totalColor = new Color(r, g, b, 1);
        colorDisplay.color = totalColor;
        geneSheepManager.startColor = totalColor;
        geneSheepManager.randomStartColor = randomColorToggle.isOn;

    }

    
    void OnUpdatesPerFrameChanged() {
        geneSheepManager.timeStepsPerFrame = (int)updatesPerFrame.value;
    }

    void OnSaveButtonClicked() {
        geneSheepManager.queuedSave = true;
    }

    void OnRestartButtonClicked() {
        geneSheepManager.queuedRestart = true;
    }

    void OnAutoRestartChanged() {
        geneSheepManager.autoRestart = autoRestartToggle.isOn;
    }

    void OnSavePixelInfoChanged() {
        geneSheepManager.savePixelInfo = savePixelInfoToggle.isOn;
    }

    void OnDimensionsChanged() {
        int width = (int) math.pow(2, widthSlider.value);
        int height = (int) math.pow(2, heightSlider.value);
        geneSheepManager.initWidth = width;
        geneSheepManager.initHeight = height;
        widthSliderLabel.text = "Width: " + width.ToString();
        heightSliderLabel.text = "Height: " + height.ToString();

    }

    void OnSpeciesChanged() {
        geneSheepManager.numSpecies = (int) speciesSlider.value;
        speciesSliderLabel.text = "Species: " + speciesSlider.value.ToString();
    }

    void OnPixelChangeScaleChanged() {
        geneSheepManager.pixelChangeScale = (float) 0.00001 * math.pow(1.5f, pixelChangeScaleSlider.value);
    }

    void OnUpdateRuleChanged() {
        geneSheepManager.updateRule = updateRule.value;
    }
}
