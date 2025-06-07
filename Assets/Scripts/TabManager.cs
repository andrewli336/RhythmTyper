using UnityEngine;

public class TabManager : MonoBehaviour
{
    public GameObject timingTab;
    public GameObject objectsTab;
    public GameObject setupTab;
    void Start()
    {
        ShowTimingTab();
    }
    public void ShowTimingTab()
    {
        timingTab.SetActive(true);
        objectsTab.SetActive(false);
        setupTab.SetActive(false);
    }

    public void ShowObjectsTab()
    {
        timingTab.SetActive(false);
        objectsTab.SetActive(true);
        setupTab.SetActive(false);
    }

    public void ShowSetupTab()
    {
        timingTab.SetActive(false);
        objectsTab.SetActive(false);
        setupTab.SetActive(true);
    }
    public bool IsObjectsTabActive()
    {
        return objectsTab != null && objectsTab.activeSelf;
    }

}