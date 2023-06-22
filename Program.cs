using System.Text.Json;
using System.Collections;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

// Load example data
var alarmLog = JsonSerializer.Deserialize<IReadOnlyList<AlarmLog>>(File.ReadAllText("alarmlog.json"));
var alarms = JsonSerializer.Deserialize<IReadOnlyList<Alarm>>(File.ReadAllText("alarms.json"));

// Set up endpoints
app.MapGet("/alarms", () =>
{
    var models = new List<AlarmModel>();
    foreach (var alarm in alarms)
    {
        models.Add(
            new AlarmModel
            {
                Id = alarm.AlarmId,
                Station = alarm.Station,
                Number = alarm.AlarmNumber,
                Class = alarm.AlarmClass,
                Text = alarm.AlarmText,
            }
        );
    }

    return models;
})
.WithName("Alarms");


app.MapGet("/alarmLog", () =>
{
    return alarmLog;
})
.WithName("AlarmLog");


app.MapGet("/most-activated-stations", () =>
{
    //creating a dictionary for every sort of alarm so I can easily connect saved id's
    //from alarmlog to type of alarm
    IDictionary<int, AlarmModel> alarmModels = new Dictionary<int, AlarmModel>();

    foreach (var alarm in alarms)
    {
        AlarmModel tempAlarm = new AlarmModel
        {
            Id = alarm.AlarmId,
            Station = alarm.Station,
            Number = alarm.AlarmNumber,
            Class = alarm.AlarmClass,
            Text = alarm.AlarmText,
        };
        alarmModels.Add(alarm.AlarmId,tempAlarm);
    }

    //creating a list from alarmlog but only saving the ones that has a LoggedAlarmEvent = on
    var models = new List<AlarmLog>();
    foreach (var alarm in alarmLog)
    {
        var temp = new AlarmLog
        {
            AlarmId = alarm.AlarmId,
            Event = alarm.Event,
            AckBy = alarm.AckBy,
            Date = alarm.Date,
        };

        LoggedAlarmEvent curr = temp.Event;

        if (curr == LoggedAlarmEvent.On) { 
            models.Add(temp);
        }
    }

    //creating Dictionary containing only keys from alarmIds present in the models-list
    //here the key is an alarmId and the value is number of times it has activated
    //if the same alarmId is to be added I only increase the value with + 1
    IDictionary<int, int> calledAlarms = new Dictionary<int, int>();

    foreach (var alarm in models)
    {
        if (calledAlarms.ContainsKey(alarm.AlarmId))
        {
         calledAlarms[alarm.AlarmId] = (int)calledAlarms[alarm.AlarmId] + 1;
        }
        else
        {
            calledAlarms.Add(alarm.AlarmId,1);
        }
    }

    //sorting the calledAlarms Dictionary and putting it in a new variable sortedDict
    var sortedDict = from entry in calledAlarms orderby entry.Value descending select entry;


    //create a list with strings that render out station, message for that alarm and the number of calls
    //it compares the alarmIDs in sortedDict with a corresponding Station and Message in 
    //the dictionary alarmModels, then adds them all together.
    var outPutStrings = new List<String>();

    foreach (var x in sortedDict)
    {  
        var tempo = alarmModels[x.Key];

        String tempi = tempo.Station;

        String fixes = "Station " + tempo.Station + ", " + tempo.Text + ", " + x.Value;

        outPutStrings.Add(fixes);
    }

        return outPutStrings;   //OUTPUT
})
.WithName("MAS");
//end of MAS





app.MapGet("/activations-per-station", () =>
{
    //creating a dictionary for every sort of alarm so I can easily connect saved id's
    //from alarmlog to type of alarm
    IDictionary<int, AlarmModel> alarmModels = new Dictionary<int, AlarmModel>();

    foreach (var alarm in alarms)
    {
        AlarmModel tempAlarm = new AlarmModel
        {
            Id = alarm.AlarmId,
            Station = alarm.Station,
            Number = alarm.AlarmNumber,
            Class = alarm.AlarmClass,
            Text = alarm.AlarmText,
        };
        alarmModels.Add(alarm.AlarmId, tempAlarm);
    }

    //creating a dictionary and entering every station from the log referenced in alarmModels as a new key
    //if the key allready exist I only add +1 to the value
    IDictionary<String, int> totalCalledAlarms = new Dictionary<String, int>();

    foreach (var log in alarmLog)
    {
        var tempo = alarmModels[log.AlarmId];

        var keyName =  tempo.Station; 

        if (totalCalledAlarms.ContainsKey(keyName))
        {
            totalCalledAlarms[keyName] = (int)totalCalledAlarms[keyName] + 1;
        }
        else
        {
            totalCalledAlarms.Add(keyName, 1);
        }
    }

    //sorting the list by the station with most activations at the top
    var sortedDict = from entry in totalCalledAlarms orderby entry.Value descending select entry;

    //fixing a list of strings that presents the result
    var outPutStrings = new List<String>();

    foreach (var x in sortedDict)
    {
        String fixes = "Station " + x.Key + ", " + x.Value;

        outPutStrings.Add(fixes);
    }

    return outPutStrings;   //OUTPUT
})
.WithName("APS");
//end of APS




app.MapGet("/current-system-status", () =>
{

    //create a dictionary where every event is a key, then add +1 to number of times it has happened if
    //the key already exist
    IDictionary<LoggedAlarmEvent, int> totalCalledEvents = new Dictionary<LoggedAlarmEvent, int>();

    foreach (var log in alarmLog)
    {

        if (totalCalledEvents.ContainsKey(log.Event))
        {
            totalCalledEvents[log.Event] = (int)totalCalledEvents[log.Event] + 1;
        }
        else
        {
            totalCalledEvents.Add(log.Event, 1);
        }
    }

    return totalCalledEvents;   //OUTPUT
})
.WithName("CSS");
//end of CSS




app.Run();



// Output models
public class AlarmModel
{
    public int Id { get; set; }

    public string Station { get; set; }

    public int Number { get; set; }

    public string Class { get; set; }

    public string Text { get; set; }
}


// Data models
public record AlarmLog
{
    public int AlarmId { get; init; }

    public LoggedAlarmEvent Event { get; init; }

    public string AckBy { get; init; }

    public DateTime Date { get; init; }
}

public record Alarm
{
    public int AlarmId { get; init; }

    public string Station { get; init; }

    public int AlarmNumber { get; init; }

    public string AlarmClass { get; init; }

    public string AlarmText { get; init; }
}

public enum LoggedAlarmEvent
{
    Off = 0,
    On = 1,
    Acked = 2,
    Blocked = 3,
    UnBlocked = 4,
    AckedLocally = 5,
    Cause = 6,
    Reset = 7,
    PagingSentToUser = 8,
    PagingUserSMSReceived = 9,
    PagingSentSMSToUser = 10,
    PagingSentMailToUser = 11,
    PagingSentPushToUser = 12,
    PagingUserPushReceived = 13,
    PagingUserPushRead = 14,
}