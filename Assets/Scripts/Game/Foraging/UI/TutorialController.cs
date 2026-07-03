using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

public class TutorialController : MonoBehaviour
{
    #region fields

    [Header("References")]
    [SerializeField] private GameObject tutorialPannel;
    [SerializeField] private GameObject leftButton;
    [SerializeField] private GameObject rightButton;

    [Header("Video settings")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private List<VideoClip> tutorialClips;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private List<string> titles;
    private int curFrame = 0;

    #endregion


    #region initialization

    void Awake()
    {
        leftButton.SetActive(false);
    }

    #endregion


    #region controls

    public void Show()
    {
        tutorialPannel.SetActive(true);
        showSlide(curFrame);
    }

    public void Hide()
    {
        tutorialPannel.SetActive(false);
    }

    private void showSlide(int page)
    {
        videoPlayer.clip = tutorialClips[page];
        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += PlayVideo;

        label.text = titles[page];
    }

    private void PlayVideo(VideoPlayer vp)
    {
        videoPlayer.prepareCompleted -= PlayVideo;
        vp.Play();
    }

    public void NextTutorialFrame()
    {
        curFrame++;

        leftButton.SetActive(true);
        if (curFrame == tutorialClips.Count - 1)
        {
            rightButton.SetActive(false);
        }

        showSlide(curFrame);
    }

    public void PreviousTutorialFrame()
    {
        curFrame--;

        rightButton.SetActive(true);
        if (curFrame == 0)
        {
            leftButton.SetActive(false);
        }

        showSlide(curFrame);
    }

    #endregion
}
