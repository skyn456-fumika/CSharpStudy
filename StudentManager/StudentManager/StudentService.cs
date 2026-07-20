public class StudentService
{
    private readonly List<Student> students = new();

    private int nextId = 1;

    public void AddStudent(
        string name,
        int korean,
        int english,
        int math)
    {
        Student student = new Student
        {
            Id = nextId++,
            Name = name,
            Korean = korean,
            English = english,
            Math = math
        };

        students.Add(student);
    }

    public List<Student> GetAllStudents()
    {
        return students
            .OrderBy(student => student.Id)
            .ToList();
    }

    public Student? FindById(int id)
    {
        return students
            .FirstOrDefault(student => student.Id == id);
    }

    public List<Student> SearchByName(string name)
    {
        return students
            .Where(student =>
                student.Name.Contains(
                    name,
                    StringComparison.OrdinalIgnoreCase))
            .OrderBy(student => student.Name)
            .ToList();
    }

    public bool DeleteStudent(int id)
    {
        Student? student = FindById(id);

        if (student == null)
        {
            return false;
        }

        students.Remove(student);

        return true;
    }
}