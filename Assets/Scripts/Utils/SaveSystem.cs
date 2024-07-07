using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Newtonsoft.Json;
using SFB;
using System.Text;

[Serializable]
public class LevelData{
    public List<ColorSampleData> colorData;
    public List<Beaker> beakerData;
    public int maxCapacity;
}

public class SaveSystem : MonoBehaviour
{
    private readonly string savePath = "/Save";
    private readonly string extension = "wsp";
    [SerializeField]
    private ButtonOpenFile loadButton;

    [SerializeField]
    private GameObject go_dialogBox;
    [SerializeField]
    private GameObject go_saveSpecifficElements;
    [SerializeField]
    private GameObject go_loadSpecifficElements;
    [SerializeField]
    private GameObject go_deleteSpecifficElements;

    [SerializeField]
    private FileContainer fileNamesContainer;
    [SerializeField]
    private GameObject go_fileNameInputField;

    [SerializeField]
    private DialogHUD dialogHUD;
#if UNITY_WEBGL && !UNITY_EDITOR
    //
    // WebGL
    //
    [DllImport("__Internal")]
    private static extern void DownloadFile(string gameObjectName, string methodName, string filename, byte[] byteArray, int byteArraySize);

    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);
#endif


    private void Start()
    {
        loadButton.registLoadFileCb((url)=>{
            Debug.Log("------load url = " + url);
            StartCoroutine(OutputRoutine(url));
        });
        fileNamesContainer.onElementSelected += ClearInputField;
    }

    // display

    public void DisplaySaveDialog()
    {
        // display UI
        go_dialogBox.SetActive(true);
        go_saveSpecifficElements.SetActive(true);
        StartCoroutine(DisplayFileList());
    }

    public void DisplayLoadDialog()
    {
        // display UI
        go_dialogBox.SetActive(true);
        go_loadSpecifficElements.SetActive(true);
        StartCoroutine(DisplayFileList());
    }

    public void DisplayDeleteDialog()
    {
        // display UI
        go_dialogBox.SetActive(true);
        go_deleteSpecifficElements.SetActive(true);
        StartCoroutine(DisplayFileList());
    }

    private IEnumerator DisplayFileList()
    {
        yield return new WaitForSeconds(0.1f);
        PopulateFileList();
    }

    private void PopulateFileList()
    {
        fileNamesContainer.ResetContents();
        var files = GetSaveFileNames();

        if (files.Count > 0)
        {
            fileNamesContainer.Display(files);
        }
    }

    private List<string> GetSaveFileNames()
    {
        var fileNames = new List<string>();
        string searchPattern = $"*.{extension}";
        try
        {
            var files = new DirectoryInfo(Application.persistentDataPath + savePath).GetFiles(searchPattern, SearchOption.AllDirectories);

            foreach (var file in files)
            {
                fileNames.Add(file.Name.TrimEnd(('.' + extension).ToCharArray()));
            }
        }
        catch
        {
            // directory doesn't exist
        }

        return fileNames;
    }


    // buttons

    public void OnSavePressed()
    {
        // create file
        // string fileName = GetSelectedFileName();
        // if (fileName == string.Empty)
        // {
        //     dialogHUD.Display("You must provide a name for the file. Either by picking a file that already exists or by typing one.", "Close");
        //     return;
        // }

        // var data = Tuple.Create(ColorContainer.Instance.GetData(), BeakerContainer.Instance.GetData(), BeakerUI.MaxCapacity);
        // object[] elements = {data.Item1,data.Item2,data.Item3};

        LevelData levelData = new LevelData();
        levelData.colorData = ColorContainer.Instance.GetData();
        levelData.beakerData = BeakerContainer.Instance.GetData();
        levelData.maxCapacity = BeakerUI.MaxCapacity;
        
        string jsonstr = JsonConvert.SerializeObject(levelData);
        Debug.Log("jsonstr = " + jsonstr);
        string filename = go_fileNameInputField.GetComponent<TMPro.TMP_InputField>().text;
        byte[] bytes = Encoding.UTF8.GetBytes(jsonstr);
#if UNITY_WEBGL && !UNITY_EDITOR
        DownloadFile("Save File","OnFileDownload",$"{filename}.json.txt",bytes,bytes.Length);
#else
        var extensions = new [] {
        new ExtensionFilter("Text Files", "txt" )};
        string path = StandaloneFileBrowser.SaveFilePanel("Save File","./",filename + ".json",extensions);
        if(string.IsNullOrEmpty(path)){
            return;
        }
        File.WriteAllText(path,jsonstr);
#endif
        


        // SaveData(data, fileName);
        dialogHUD.Display("Saved.", "Close");

        ClearInputField();

        // hide UI
        go_dialogBox.SetActive(false);
        go_saveSpecifficElements.SetActive(false);
    }

    private IEnumerator OutputRoutine(string url) {
        var loader = new WWW(url);
        yield return loader;
        string content = loader.text;
        Debug.Log("content = " + content);
        Dictionary<string,object> dic = MiniJSON.Json.Deserialize(content) as Dictionary<string,object>;
        List<object> colorObjectList = dic["colorData"] as List<object>;
        List<object> beakerObjectList = dic["beakerData"] as List<object>;
        int maxCapacity = (int)dic["maxCapacity"];

        List<ColorSampleData> colorList = new List<ColorSampleData>();
        List<Beaker> beakerList = new List<Beaker>();
        
        for(int i = 0; i < colorObjectList.Count; i++){
            Dictionary<string,object> dicColor = colorObjectList[i] as Dictionary<string,object>;
            int id = (int)dicColor["id"];
            float r = (float)dicColor["r"];
            float g = (float)dicColor["g"];
            float b = (float)dicColor["b"];
            float a = (float)dicColor["a"];
            ColorSampleData colorSampleData = new ColorSampleData(){id = id,r = r,g = g,b = b,a = a};
            colorList.Add(colorSampleData);
        }

        for(int i = 0; i < beakerObjectList.Count; i++){
            Dictionary<string,object> dicBeaker = beakerObjectList[i] as Dictionary<string,object>;
            List<object> lstContent = dicBeaker["Contents"] as List<object>;
            List<int> lstContentInt = new List<int>();
            for(var j = 0; j < lstContent.Count; j++){
                lstContentInt.Add((int)lstContent[j]);
            }
            Beaker beaker = new Beaker(lstContentInt);
            beakerList.Add(beaker);
        }

        StartCoroutine(LoadData(colorList,beakerList,maxCapacity));

        // LevelData levelData = Newtonsoft.Json.JsonConvert.DeserializeObject<LevelData>(content);
        // StartCoroutine(LoadData(levelData.colorData,levelData.beakerData,levelData.maxCapacity));
    }

    public void OnFileUpload(string url) {
        if(string.IsNullOrEmpty(url)){
            return;
        }
        StartCoroutine(OutputRoutine(url));
    }

    public void OnLoadPressed()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        UploadFile("FileInput", "OnFileUpload", ".txt", false);
#else
        var extensions = new [] {new ExtensionFilter("Text Files", "txt" )};
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Load File","./",extensions,false);
        if(paths.Length == 0){
            return;
        }
        StartCoroutine(OutputRoutine(paths[0]));
#endif
    }

    public void OnDeletePressed()
    {
        var fileName = fileNamesContainer.SelectedItem;
        if (fileName != string.Empty)
        {
            File.Delete(GetFullPath(fileName));
            fileNamesContainer.DeleteSelectedElement();
        }
        else
        {
            dialogHUD.Display("No file was selected", "Close");
        }
    }

    public void OnCancelPressed()
    {
        ClearInputField();
        go_saveSpecifficElements.SetActive(false);
        go_loadSpecifficElements.SetActive(false);
        go_dialogBox.SetActive(false);
    }


    // functionality

    private IEnumerator LoadData(List<ColorSampleData> colorData, List<Beaker> beakerData, int maxCapacity)
    {
        // gather data

        // load fill the containers
        try
        {
            ColorContainer.Instance.LoadData(colorData);
        }
        catch // the data wasn't loaded correctly
        {
            // reset the container
            ColorContainer.Instance.ResetContents();

            dialogHUD.Display("Couldn't load the configuration (color samples) from the given file.", "Close");
            yield break;
        }

        yield return new WaitForSeconds(0.1f);

        try
        {
            BeakerContainer.Instance.LoadData(beakerData, maxCapacity);

            // the data was loaded correctly
            dialogHUD.Display("Success.", "Close");
        }
        catch // the data wasn't loaded correctly
        {
            // reset the containers
            BeakerContainer.Instance.ResetContents();
            ColorContainer.Instance.ResetContents();

            dialogHUD.Display("Couldn't load the configuration (beakers) from the given file.", "Close");
            yield break;
        }

        ClearInputField();
        // hide UI
        go_dialogBox.SetActive(false);
        go_loadSpecifficElements.SetActive(false);
    }

    private void SaveData(Tuple<List<ColorSampleData>, List<Beaker>, int> data, string fileName)
    {
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        string path = GetFullPath(fileName);
        EnsureFolder(Application.persistentDataPath + savePath);
        FileStream stream = new FileStream(path, FileMode.Create);

        binaryFormatter.Serialize(stream, data);
        stream.Close();
    }

    private Tuple<List<ColorSampleData>, List<Beaker>, int> LoadData(string fileName)
    {
        string path = GetFullPath(fileName);

        if (File.Exists(path))
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            var data = binaryFormatter.Deserialize(stream) as Tuple<List<ColorSampleData>, List<Beaker>, int>;
            stream.Close();

            return data;
        }

        throw new FileNotFoundException();
    }

    private void EnsureFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            string directoryName = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directoryName);
        }
    }

    private string GetFullPath(string fileName)
    {
        return Application.persistentDataPath + savePath + '/' + fileName + '.' + extension;
    }

    private string GetSelectedFileName()
    {
        var textInput = go_fileNameInputField.GetComponent<TMPro.TMP_InputField>().text;

        if (textInput != string.Empty)
        {
            return textInput;
        }

        return fileNamesContainer.SelectedItem;
    }

    private void ClearInputField()
    {
        go_fileNameInputField.GetComponent<TMPro.TMP_InputField>().text = string.Empty;
    }
}
