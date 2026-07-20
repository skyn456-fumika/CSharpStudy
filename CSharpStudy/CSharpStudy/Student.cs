public class Student
{
    public string Name { get; set; } = "";
    public int Korean { get; set; }
    public int English { get; set; }
    public int Math { get; set; }

    public int GetTotal()
    {
        return Korean + English + Math;
    }

    public double GetAverage()
    {
        return GetTotal() / 3.0;
    }

    public bool IsPassed()
    {
        return GetAverage() >= 60;
    }

    public void PrintResult()
    {
        Console.WriteLine($"학생 이름: {Name}");
        Console.WriteLine($"총점: {GetTotal()}");
        Console.WriteLine($"평균: {GetAverage():F1}");
        Console.WriteLine($"결과: {(IsPassed() ? "합격" : "불합격")}");
    }
}