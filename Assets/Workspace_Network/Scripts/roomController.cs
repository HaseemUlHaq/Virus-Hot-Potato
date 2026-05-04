using UnityEngine;

public class roomController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void showRoom()
    {
        //self.transform = Vector3(-20,0,0);
        transform.position = new Vector3(0,1.7f,0);
    }

    public void hideRoom()
    {
        //self.transform = Vector3(-20,0,0);
        transform.position = new Vector3(-20,1.7f,0);
    }

}
