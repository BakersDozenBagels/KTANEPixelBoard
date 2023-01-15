using System.Collections;
using UnityEngine;

[RequireComponent(typeof(KMSelectable))]
public class PixelScript : MonoBehaviour
{
    [SerializeField, RummageNoRemove, RummageNoRename]
    private Renderer _base;

    private bool _highlighted;
    private Color _setColor;
    private static readonly Color s_hlColor = new Color(0.4f, .13f, 0.74f);

    public Color Color
    {
        set
        {
            _setColor = value;
            if(!_highlighted)
                ShowColor(value);
        }
    }

    [RummageNoRemove, RummageNoRename]
    private void Start()
    {
        Color = Color.black;
        GetComponent<KMSelectable>().OnHighlight += Highlight;
        GetComponent<KMSelectable>().OnHighlightEnded += Unhighlight;
        GetComponent<KMSelectable>().OnInteract += Interact;
    }

    private bool Interact()
    {
        GetComponent<KMSelectable>().AddInteractionPunch(0.1f);
        GetComponentInParent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponentInParent<KMAudio>().PlaySoundAtTransform("Press", transform);
        StartCoroutine(Animate());
        return false;
    }

    private IEnumerator Animate()
    {
        float t = Time.time;
        while(Time.time - t < 0.1f)
        {
            yield return null;
            float l = (Time.time - t) * 10f;
            transform.GetChild(1).localPosition = new Vector3(0f, Mathf.Lerp(0f, -0.003f, l), 0f);
        }
        while(Time.time - t < 0.2f)
        {
            yield return null;
            float l = (Time.time - t) * 10f - 1f;
            transform.GetChild(1).localPosition = new Vector3(0f, Mathf.Lerp(-0.003f, 0f, l), 0f);
        }
        transform.GetChild(1).localPosition = Vector3.zero;
    }

    private void Unhighlight()
    {
        _highlighted = false;
        ShowColor(_setColor);
    }

    private void Highlight()
    {
        _highlighted = true;
        ShowColor(s_hlColor);
    }

    private void ShowColor(Color c)
    {
        _base.materials[2].color = c;
    }
}
