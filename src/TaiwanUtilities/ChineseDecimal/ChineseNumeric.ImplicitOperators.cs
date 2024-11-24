namespace TaiwanUtilities;
partial struct ChineseNumeric
{
    public static implicit operator decimal(ChineseNumeric chineseNumeric)
    {
        return chineseNumeric.GetRawValue();
    }

    public static implicit operator ChineseNumeric(decimal value)
    {
        return new(value);
    }

}
