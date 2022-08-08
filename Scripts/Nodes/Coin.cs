using Godot;

namespace Fifteen.Nodes 
{
    public class Coin : Area
    {
        public override void _Process(float delta)
        {
            RotateObjectLocal(new Vector3(1f, 0, 0), 3f * delta);
        }

        private void BodyEntered(Node body)
        {
            if (body is RigidBody)
            {
                this.RemoveFromGroup("Coins");
                this.QueueFree();
                Rotacube.Singleton.EmitSignal("CoinCollected");
            }
        }
    }
}