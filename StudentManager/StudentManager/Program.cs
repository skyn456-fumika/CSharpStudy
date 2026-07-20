StudentService studentService = new StudentService();

bool isRunning = true;

while (isRunning)
{
    PrintMenu();

    Console.Write("메뉴를 선택하세요: ");
    string? menu = Console.ReadLine();

    Console.WriteLine();

    switch (menu)
    {
        case "1":
            AddStudent(studentService);
            break;

        case "2":
            PrintStudents(studentService.GetAllStudents());
            break;

        case "3":
            SearchStudents(studentService);
            break;

        case "4":
            DeleteStudent(studentService);
            break;

        case "0":
            isRunning = false;
            Console.WriteLine("프로그램을 종료합니다.");
            break;

        default:
            Console.WriteLine("올바른 메뉴를 선택하세요.");
            break;
    }

    Console.WriteLine();
}

static void PrintMenu()
{
    Console.WriteLine("======================");
    Console.WriteLine("     학생 관리 프로그램");
    Console.WriteLine("======================");
    Console.WriteLine("1. 학생 등록");
    Console.WriteLine("2. 학생 목록");
    Console.WriteLine("3. 학생 검색");
    Console.WriteLine("4. 학생 삭제");
    Console.WriteLine("0. 종료");
    Console.WriteLine("======================");
}

static void AddStudent(StudentService studentService)
{
    Console.WriteLine("[학생 등록]");

    Console.Write("이름: ");
    string name = Console.ReadLine()?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(name))
    {
        Console.WriteLine("이름을 입력해야 합니다.");
        return;
    }

    int? korean = ReadScore("국어 점수: ");

    if (korean == null)
    {
        return;
    }

    int? english = ReadScore("영어 점수: ");

    if (english == null)
    {
        return;
    }

    int? math = ReadScore("수학 점수: ");

    if (math == null)
    {
        return;
    }

    studentService.AddStudent(
        name,
        korean.Value,
        english.Value,
        math.Value);

    Console.WriteLine("학생이 등록되었습니다.");
}

static int? ReadScore(string message)
{
    Console.Write(message);

    if (!int.TryParse(Console.ReadLine(), out int score))
    {
        Console.WriteLine("점수는 숫자로 입력해야 합니다.");
        return null;
    }

    if (score < 0 || score > 100)
    {
        Console.WriteLine("점수는 0점부터 100점 사이여야 합니다.");
        return null;
    }

    return score;
}

static void PrintStudents(List<Student> students)
{
    Console.WriteLine("[학생 목록]");

    if (students.Count == 0)
    {
        Console.WriteLine("등록된 학생이 없습니다.");
        return;
    }

    foreach (Student student in students)
    {
        PrintStudent(student);
    }
}

static void SearchStudents(StudentService studentService)
{
    Console.WriteLine("[학생 검색]");

    Console.Write("검색할 이름: ");
    string keyword = Console.ReadLine()?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(keyword))
    {
        Console.WriteLine("검색어를 입력해야 합니다.");
        return;
    }

    List<Student> students =
        studentService.SearchByName(keyword);

    if (students.Count == 0)
    {
        Console.WriteLine("검색 결과가 없습니다.");
        return;
    }

    PrintStudents(students);
}

static void DeleteStudent(StudentService studentService)
{
    Console.WriteLine("[학생 삭제]");

    Console.Write("삭제할 학생 ID: ");

    if (!int.TryParse(Console.ReadLine(), out int id))
    {
        Console.WriteLine("ID는 숫자로 입력해야 합니다.");
        return;
    }

    Student? student = studentService.FindById(id);

    if (student == null)
    {
        Console.WriteLine("해당 학생을 찾을 수 없습니다.");
        return;
    }

    PrintStudent(student);

    Console.Write("정말 삭제하시겠습니까? (y/n): ");
    string answer = Console.ReadLine()?.Trim().ToLower() ?? "";

    if (answer != "y")
    {
        Console.WriteLine("삭제를 취소했습니다.");
        return;
    }

    bool deleted = studentService.DeleteStudent(id);

    Console.WriteLine(
        deleted
            ? "학생이 삭제되었습니다."
            : "학생 삭제에 실패했습니다.");
}

static void PrintStudent(Student student)
{
    Console.WriteLine("--------------------------------");
    Console.WriteLine($"ID: {student.Id}");
    Console.WriteLine($"이름: {student.Name}");
    Console.WriteLine(
        $"국어: {student.Korean}, " +
        $"영어: {student.English}, " +
        $"수학: {student.Math}");
    Console.WriteLine($"총점: {student.Total}");
    Console.WriteLine($"평균: {student.Average:F1}");
    Console.WriteLine(
        $"결과: {(student.IsPassed ? "합격" : "불합격")}");
}