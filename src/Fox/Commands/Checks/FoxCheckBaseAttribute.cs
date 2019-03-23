using Qmmands;

namespace Fox.Commands.Checks
{
    public abstract class FoxCheckBaseAttribute : CheckBaseAttribute
    {
        public abstract string Name { get; set; }
        public abstract string Details { get; set; }
    }
}
