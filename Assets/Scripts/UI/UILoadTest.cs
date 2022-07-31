using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UILoadTest : MonoBehaviour
{
    public Button startLoadBtn;
    public Text waitLoadRequest;
    public Text waitOtherLoadRequest;
    public Text abActiveLoadRequest;
    public Text abWaitLoadRequest;
    public Text waitDepRequest;
    public Text assetWaitLoadRequest;
    public Text assetActiveLoadRequest;

    AsyncOperation m_loadSceneReq;
    public void OnStartLoadBtn()
    {
        startLoadBtn.gameObject.SetActive(false);
        m_loadSceneReq = SceneManager.LoadSceneAsync("CityScene_streaming", LoadSceneMode.Additive);
        //StartCoroutine(UpdateDebugInfoCoroutine());
    }

    private void Update()
    {
        UpdateDebugInfo();
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
    }
}
