using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Emits a notification when a plant is watered without tracking plant hydration.
/// A later plant-care system can subscribe to this component without changing the can.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlantWaterReceiver : MonoBehaviour
{
    [SerializeField] private UnityEvent<float> onWaterReceived = new();

    /// <summary>
    /// Raised with the amount of water received in litres.
    /// </summary>
    public event Action<float> WaterReceived;

    public void ReceiveWater(float amountLitres)
    {
        if (amountLitres <= 0f)
        {
            return;
        }

        WaterReceived?.Invoke(amountLitres);
        onWaterReceived.Invoke(amountLitres);
    }
}
