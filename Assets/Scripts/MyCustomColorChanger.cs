using System.Collections;

using UnityEngine;



public class MyCustomColorChanger : MonoBehaviour

{



    [SerializeField]

    private Color normalColor = Color.red;

    [SerializeField]

    private Color hoveredColor = Color.blue;

    [SerializeField]

    private Color selectedColor = Color.green;



    [SerializeField]

    private float transitionTime = 0.5f;



    [SerializeField]

    private Renderer objectRenderer;



    private Material mat;

    private Coroutine currentLerp;



    void Awake()

    {

        mat = objectRenderer.material;

    }



    public void TransitionToColor(Color targetColor)

    {



        if (currentLerp != null)

            StopCoroutine(currentLerp);



        currentLerp = StartCoroutine(LerpColor(targetColor, transitionTime));

    }



    private IEnumerator LerpColor(Color targetColor, float duration)

    {

        Color startColor = mat.color;

        float time = 0f;



        while (time < duration)

        {

            time += Time.deltaTime;

            float t = time / duration;



            t = Mathf.SmoothStep(0f, 1f, t);



            mat.color = Color.Lerp(startColor, targetColor, t);

            yield return null;

        }



        mat.color = targetColor;

    }



    public void Transition2Hover()

    {

        TransitionToColor(hoveredColor);

    }



    public void Transition2Select()

    {

        TransitionToColor(selectedColor);

    }



    public void Transition2Normal()

    {

        TransitionToColor(normalColor);

    }




}