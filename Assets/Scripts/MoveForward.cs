using System.Collections;
using UnityEngine;

public class MoveForward : MonoBehaviour
{
    /// <summary>
    /// Moves the specified transform to the right by 1 tile (16 pixels) over time
    /// </summary>
    /// <param name="objectTransform">The transform to move</param>
    /// <param name="duration">Duration of the movement in seconds</param>
    /// <returns>Coroutine for the movement</returns>
    public static IEnumerator MoveRightByOneTile(Transform objectTransform)
    {
        float duration = 0.5f;
        
        if (objectTransform == null)
        {
            Debug.LogError("Transform is null - cannot move object");
            yield break;
        }

        Vector3 startPosition = objectTransform.position;
        Vector3 endPosition = startPosition + Vector3.right * Constants.TileSize; // 16 pixels to the right
        
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            // Use smooth interpolation
            objectTransform.position = Vector3.Lerp(startPosition, endPosition, progress);
            
            yield return null; // Wait for next frame
        }
        
        // Ensure we reach the exact end position
        objectTransform.position = endPosition;
    }
    
    /// <summary>
    /// Helper method to start the movement coroutine on a MonoBehaviour
    /// </summary>
    /// <param name="monoBehaviour">MonoBehaviour to start the coroutine on</param>
    /// <param name="objectTransform">The transform to move</param>
    /// <param name="duration">Duration of the movement in seconds</param>
    public static Coroutine StartMoveRightByOneTile(MonoBehaviour monoBehaviour, Transform objectTransform)
    {
        if (monoBehaviour == null)
        {
            Debug.LogError("MonoBehaviour is null - cannot start coroutine");
            return null;
        }
        
        return monoBehaviour.StartCoroutine(MoveRightByOneTile(objectTransform));
    }
}