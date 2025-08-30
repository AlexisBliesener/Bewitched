/// <summary>
/// This is the enemy health model which inherit from the base health model
/// We can use it later to extend the enemy health logic 
/// and it can be different from the other models  
/// </summary>
public class EnemyHealthModel : HealthModel
{
    public EnemyHealthModel(float maxHealth, float decayRate) : base(maxHealth, decayRate) { }
}
