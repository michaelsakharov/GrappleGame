using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonErection : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    Button butt;
    Toggle tog;

    public float TweenSpeed = 5f;
    public float OnHoverSize = 1.2f;
    public float OnClickedSize = 0.85f;
    public float ClickedDuration = 0.25f;

    float clickTimer = 0f;
    bool isHovered = false;
    Vector3 targetSize;
    RectTransform target;

    void Start()
    {
        butt = GetComponent<Button>();
        if(butt != null)
            butt.onClick.AddListener(OnClick);
        tog = GetComponent<Toggle>();
        if (tog != null)
            tog.onValueChanged.AddListener((e) => { OnClick(); });
        // Get child rectTransform for target
        //target = transform.GetChild(0).GetComponent<RectTransform>();
        //if (target == null)
            target = GetComponent<RectTransform>();
    }

    private void OnClick()
    {
        clickTimer = 0.1f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }


    void Update()
    {
        targetSize = Vector3.one;

        // If Clicked
        if (clickTimer > 0)
        {
            clickTimer -= Time.deltaTime;
            targetSize = new Vector3(OnClickedSize, OnClickedSize, 1f);
            target.transform.localScale = targetSize;
        }
        else if (isHovered)
        {
            targetSize = new Vector3(OnHoverSize, OnHoverSize, 1f);
        }
        else
        {
            targetSize = new Vector3(1f, 1f, 1f);
        }

        if (butt != null && !butt.interactable)
            targetSize = Vector3.one;

        if (tog != null && !tog.interactable)
            targetSize = Vector3.one;

        target.transform.localScale = Vector3.Lerp(target.transform.localScale, targetSize, Time.deltaTime * TweenSpeed);
    }
}
