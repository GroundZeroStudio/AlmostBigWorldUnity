using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UILoadTest : MonoBehaviour
{
    public Button startLoadBtn;
    public Button unloadBtn;
    public Text waitLoadRequest;
    public Text waitOtherLoadRequest;
    public Text abActiveLoadRequest;
    public Text abWaitLoadRequest;
    public Text waitDepRequest;
    public Text assetWaitLoadRequest;
    public Text assetActiveLoadRequest;
    public Text totalAssetCount;
    public Text waitUnloadABCount;

    AsyncOperation m_loadSceneReq;

    private void Start()
    {
        unloadBtn.gameObject.SetActive(false);
    }


    public void OnStartLoadBtn()
    {
        startLoadBtn.gameObject.SetActive(false);
        unloadBtn.gameObject.SetActive(true);
        if (SceneRootAssetReference.instance == null)
            m_loadSceneReq = SceneManager.LoadSceneAsync("CityScene_streaming", LoadSceneMode.Additive);
        else
            SceneRootAssetReference.instance.LoadAll();
    }

    public void OnUnloadBtn()
    {
        if (SceneRootAssetReference.instance != null)
        {
            SceneRootAssetReference.instance.UnloadAll();
            unloadBtn.gameObject.SetActive(false);
        }
        StartCoroutine(DelayShowLoadBtn());
    }

    private void Update()
    {
        UpdateDebugInfo();

        if (Input.GetKeyDown(KeyCode.F1))
            OnStartLoadBtn();

        if (Input.GetKeyDown(KeyCode.F2))
            OnUnloadBtn();
    }

    IEnumerator UpdateDebugInfoCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            UpdateDebugInfo();
        }
    }

    void UpdateDebugInfo()
    {
        waitLoadRequest.text = "loading ab requests:" + ResourceManager.Instance.GetLoadingRequestCount();
        waitOtherLoadRequest.text = "loading ab wait other requests:" + ResourceManager.Instance.GetABWaitOtherABRequestCount();
        abActiveLoadRequest.text = "ab active load requests:" + ResourceManager.Instance.GetABActiveRequestCount();
        abWaitLoadRequest.text = "ab wait load requests:" + ResourceManager.Instance.GetABWaitingRequestCount();
        waitDepRequest.text = "loading ab wait dep requests:" + ResourceManager.Instance.GetABWaitingRequestCount();
        assetWaitLoadRequest.text = "asset waiting requests:" + ResourceManager.Instance.GetAssetWaitingLoadRequestCount();
        assetActiveLoadRequest.text = "asset active requests:" + ResourceManager.Instance.GetAssetActiveLoadRequestCount();
        totalAssetCount.text = "total asset count:" + ResourceManager.Instance.totalAssetCount;
        waitUnloadABCount.text = "wait unload ab count:" + ResourceManager.Instance.GetWaitUnloadABCount();
    }

    IEnumerator DelayShowLoadBtn()
    {
        yield return new WaitForSeconds(5);
        startLoadBtn.gameObject.SetActive(true);
    }
}
