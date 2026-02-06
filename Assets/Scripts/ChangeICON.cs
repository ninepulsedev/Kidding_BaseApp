using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using UnityEngine.Video;

public class ChangeICON : MonoBehaviour
{
    public VideoPlayer m_VideoPlayer;
    public Sprite m_ImagePlay;
    public Sprite m_ImagePause;

    Image thisImage;

    [Button("Click", ButtonSizes.Large), GUIColor(0, 1, 1)]
    public void Click()
    {
        thisImage = GetComponent<Image>();
        print(thisImage.sprite.name);
        if (thisImage.sprite.name == m_ImagePause.name)
        {
            m_VideoPlayer.Pause();
            thisImage.sprite = m_ImagePlay;
            return;
        }

        if (thisImage.sprite.name == m_ImagePlay.name)
        {
            m_VideoPlayer.Play();
            thisImage.sprite = m_ImagePause;
            return;
        }
    }
}
