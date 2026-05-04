using UnityEngine;

public class questionMarkRotationAnim : MonoBehaviour
{
    public float rotationSpeed = 50f;

    void Update()
    {
        //transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        //transform.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
    }
}