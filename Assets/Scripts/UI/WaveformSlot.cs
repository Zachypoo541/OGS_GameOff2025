using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaveformSlot : MonoBehaviour
{
    public Image iconImage;
    public Image background;
    public TextMeshProUGUI nameText;
    public GameObject selectionBorder;

    private WaveformData waveform;

    public void Setup(WaveformData waveform, bool isSelected)
    {
        this.waveform = waveform;

        if (nameText != null)
        {
            nameText.text = waveform.waveformName;
        }

        if (iconImage != null)
        {
            iconImage.color = waveform.waveformColor;
        }

        if (background != null)
        {
            background.color = new Color(
                waveform.waveformColor.r,
                waveform.waveformColor.g,
                waveform.waveformColor.b,
                0.2f
            );
        }

        if (selectionBorder != null)
        {
            selectionBorder.SetActive(isSelected);
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectionBorder != null)
        {
            selectionBorder.SetActive(selected);
        }
    }
}