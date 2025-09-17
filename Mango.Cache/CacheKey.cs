namespace Mango.Cache
{
    class CacheKey
    {
        public string Name { get; set; }
        public string Session { get; set; }

        public override int GetHashCode()
        {
            return Name.GetHashCode() + Session.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var comparedObj = obj as CacheKey;
            return string.Equals(comparedObj.Session, this.Session) && string.Equals(comparedObj.Name, this.Name);
        }

        public override string ToString()
        {
            return $"{Session}_{Name}";
        }
    }
}
