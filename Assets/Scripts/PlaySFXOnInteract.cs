using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlaySFXOnInteract : MonoBehaviour
{
    public AudioClip interactionSFX;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.clip = interactionSFX;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            audioSource.Play();
            Debug.Log($"Memicu SFX: {interactionSFX.name} pada posisi {transform.position}");
        }
    }
}