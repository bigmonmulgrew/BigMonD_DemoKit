using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class AudioRandomizer : MonoBehaviour
{
    [Header("Pitch Settings")]
    [SerializeField] float minPitch = 0.8f;
    [SerializeField] float maxPitch = 1.2f;
    [Header("Volume Settings")]
    [SerializeField] float minVolume = 0.8f;
    [SerializeField] float maxVolume = 1.2f;
    [Header("Interval Settings")]
    [SerializeField] float minInterval = 0.5f;
    [SerializeField] float maxInterval = 1.5f;
    [SerializeField] float fadeDuration = 0.1f;

    AudioSource audioSource;
    float targetPitch;
    float targetVolume;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        StartCoroutine(RandomnizeAudio());
    }

    void Update()
    {
        if (audioSource != null)
        {
            audioSource.pitch = Mathf.Lerp(audioSource.pitch, targetPitch, Time.deltaTime / fadeDuration);
            audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume, Time.deltaTime / fadeDuration);
        }
    }
    IEnumerator RandomnizeAudio()
    {

        audioSource.Play();
        while (true)
        {

            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));

            if (audioSource != null)
            {
                targetPitch = Random.Range(minPitch, maxPitch);
                targetVolume = Random.Range(minVolume, maxVolume);
            }
        }
    }
}
