namespace GameServerManager.App.Models;

public enum ServerOverallStatus
{
    /*
        Stopped      프로세스 중지
        Starting     서버 시작 중
        Healthy      프로세스 실행 + TCP 연결 성공
        ProcessOnly  프로세스 실행 중이나 TCP 검사 전
        Unreachable  프로세스 실행 중이나 TCP 연결 실패
        Restarting   자동 재시작 중
        Crashed      프로세스 비정상 종료
    */
    Stopped,
    Starting,
    Healthy,
    ProcessOnly,
    Unreachable,
    Restarting,
    Crashed
}