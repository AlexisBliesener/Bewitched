/// <summary>
/// This is the player health model which inherit from the base health model
/// We can use it later to extend the player health logic 
/// and it can be different from other models (e.g. enemy) 
/// </summary>
public class PlayerHealthModel : HealthModel
{
    public PlayerHealthModel(float maxHealth, float decayRate) : base(maxHealth, decayRate) { }
}
