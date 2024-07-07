using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using System.IO;

[RequireComponent(typeof(Button))]
public class ButtonOpenFile : MonoBehaviour, IPointerDownHandler {
    public delegate void LoadFileCb(string text);
    LoadFileCb cb = null;
    public void registLoadFileCb(LoadFileCb cb){
        this.cb = cb;
    } 
#if UNITY_WEBGL && !UNITY_EDITOR
    //
    // WebGL
    //
    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);

    public void OnPointerDown(PointerEventData eventData) {
        string filter = ".txt";
        UploadFile(gameObject.name, "OnFileUpload", filter, false);
    }

    // Called from browser
    public void OnFileUpload(string url) {
        StartCoroutine(OutputRoutine(url));
    }
#else
    public void OnPointerDown(PointerEventData eventData) { }

    void Start() {
        var button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick() {
        string filter = "txt";
        var path = EditorUtility.OpenFilePanel("Title", "", filter);
        StartCoroutine(OutputRoutine("file:///" + path));
    }
#endif

    private IEnumerator OutputRoutine(string url) {
        var loader = new WWW(url);
        yield return loader;
        cb?.Invoke(url);
    }
}