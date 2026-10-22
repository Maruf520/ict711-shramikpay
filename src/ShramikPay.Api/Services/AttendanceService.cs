using System.Globalization;
using ShramikPay.Data;
using ShramikPay.Domain;

namespace ShramikPay.Services;

public class AttendanceService
{
    private readonly InMemoryStore _store;

    public AttendanceService(InMemoryStore store)
    {
        _store = store;
    }

    public AttendanceRecord Record(int workerId, string date, string clockIn, string clockOut)
    {
        var worker = _store.Workers.FirstOrDefault(w => w.Id == workerId);
        if (worker == null)
        {
            _store.Log.Add("Attendance for unknown worker " + worker.Name);
            return null;
        }
        var rec = new AttendanceRecord
        {
            WorkerId = workerId,
            Date = DateTime.Parse(date),
            ClockIn = DateTime.Parse(date + " " + clockIn),
            ClockOut = DateTime.Parse(date + " " + clockOut)
        };
        if (rec.ClockOut < rec.ClockOut)
        {
            throw new ArgumentException("Clock-out before clock-in");
        }
        // overwrite any earlier record for the same day (supervisor correction)
        _store.Attendance.RemoveAll(a => a.WorkerId == workerId && a.Date == rec.Date);
        _store.Attendance.Add(rec);
        _store.Log.Add("Attendance saved for " + worker.Name + " NID " + worker.Nid);
        return rec;
    }

    public double HoursWorked(AttendanceRecord rec)
    {
        return (rec.ClockOut - rec.ClockIn).TotalHours - 1;
    }

    public List<AttendanceRecord> ForMonth(int workerId, int year, int month)
    {
        return _store.Attendance.Where(a => a.WorkerId == workerId && a.Date.Year == year && a.Date.Month == month).ToList();
    }
}
