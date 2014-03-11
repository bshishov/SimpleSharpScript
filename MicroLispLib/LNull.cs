namespace MicroLispLib
{
    public class LNull : ILNode
    {
        private static LBool _boolval = new LBool(false);

        public override string ToString()
        {
            return "null";
        }
        
        public static explicit operator LBool(LNull val)
        {
            return _boolval;
        }
    }
}