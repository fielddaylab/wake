namespace Aqua
{
    public interface ILayoutAnim {
        bool IsActive();
        bool OnAnimUpdate(float deltaTime);
    }
}