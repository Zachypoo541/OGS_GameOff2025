using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    
    [Header("Health Bar")]
    public Slider healthSlider;
    public TextMeshProUGUI healthText;
    
    [Header("Energy Bar")]
    public Slider energySlider;
    public TextMeshProUGUI energyText;
    
    [Header("Waveform Display")]
    public Image waveformIcon;
    public TextMeshProUGUI waveformNameText;
    public Image waveformBackground;
    public WaveformUI waveformAnimation; // Add this line
    
    [Header("Waveform List (Optional)")]
    public Transform waveformListContainer;
    public GameObject waveformSlotPrefab;
    
    private void Start()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }
        
        if (player != null)
        {
            // Subscribe to player events
            player.OnHealthChanged += UpdateHealthBar;
            player.OnEnergyChanged += UpdateEnergyBar;
            
            // Set slider max values
            if (healthSlider != null)
            {
                healthSlider.maxValue = player.maxHealth;
                healthSlider.value = player.currentHealth;
            }
            
            if (energySlider != null)
            {
                energySlider.maxValue = player.maxEnergy;
                energySlider.value = player.currentEnergy;
            }
            
            // Initial update
            UpdateHealthBar(player.currentHealth, player.maxHealth);
            UpdateEnergyBar(player.currentEnergy, player.maxEnergy);
            UpdateWaveformDisplay();
        }
    }
    
    private void Update()
    {
        // Always update waveform display (to catch switches)
        if (player != null && player.equippedWaveform != null)
        {
            UpdateWaveformDisplay();
        }
    }
    
    private void UpdateHealthBar(float current, float max)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }
        
        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(current)} / {max}";
        }
    }
    
    private void UpdateEnergyBar(float current, float max)
    {
        if (energySlider != null)
        {
            energySlider.maxValue = max;
            energySlider.value = current;
        }
        
        if (energyText != null)
        {
            energyText.text = $"{Mathf.Ceil(current)} / {max}";
        }
    }
    
    private void UpdateWaveformDisplay()
    {
        if (player.equippedWaveform == null) return;
        
        WaveformData waveform = player.equippedWaveform;
        
        // Update waveform name
        if (waveformNameText != null)
        {
            waveformNameText.text = waveform.waveformName;
            waveformNameText.color = waveform.waveformColor;
        }
        
        // Update background color
        if (waveformBackground != null)
        {
            waveformBackground.color = new Color(
                waveform.waveformColor.r,
                waveform.waveformColor.g,
                waveform.waveformColor.b,
                0.3f
            );
        }
        
        // Update icon color (if you have one)
        if (waveformIcon != null)
        {
            waveformIcon.color = waveform.waveformColor;
        }
        
        // Update animated waveform display
        if (waveformAnimation != null)
        {
            waveformAnimation.SetWaveform(waveform);
        }
    }
    
    private void OnDestroy()
    {
        if (player != null)
        {
            player.OnHealthChanged -= UpdateHealthBar;
            player.OnEnergyChanged -= UpdateEnergyBar;
        }
    }
}
