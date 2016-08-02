# Nizcita
*Alpha - version 0.1*

Nizcita is a circuit breaker library for asycn function invocations in C#. The term Nizcita literally means "decided,ascertained" in [Sanskrit][sn]

> I have tried to make Nizcita "pluggable". Along with other features, the client can supply the function that determines, whether the circuit closes, can also supply a function to determine if a probe should be attempted (when the circuit is closed).

The examples below, try to illustrate this 'pluggable' aspect.

### Version
0.1

### Usage

The examples below illustrate some typical ways in which Nizcita can be used.

##### Providing an alternative
An alternative to the first choice can be setup by calling "Alternate" and suuplying the alternate function definition. In the example below "bing news" is used as an alternate to "google news".
```C#
static void Main(string[] args) {
    int failureThreshold = 2;
    List<Func<Point[], bool>> reducers = new List<Func<Point[], bool>>();
    reducers.Add((points) => {
        if(points.Count() > failureThreshold) {
            return true;
        }
        return false;
    });

    CircuitBreaker<HttpResponseMessage> cb = new CircuitBreaker<HttpResponseMessage>(10, reducers).Alternate((token) => {
        HttpClient client = new HttpClient();
        return client.GetAsync("https://www.bing.com/news", HttpCompletionOption.ResponseContentRead);
    }).CheckResult((r) => {
        return r.IsSuccessStatusCode;
    });

    cb.InvokeAsync((token) => {
        HttpClient client = new HttpClient();
        return client.GetAsync("https://google.com/news", HttpCompletionOption.ResponseContentRead);
    }).ContinueWith((t) => {
        Console.WriteLine("Got news: {0}",t.Result.StatusCode);
    });
    
    // do other work
    for(; Console.ReadKey().KeyChar != 'q';) {                
    }
}
```
##### When does an alternative get invoked
An alternate could get invoked if the first choice invocation fails. An invocation can be considered as fail under these circumstances:
1. The invocation results in an exception.
2. The invocation does not complete within a certain duration (This duration can be determined by the client by using WithinTime)
3. The innocations does not return the desired result (If the result is acceptable or not can be determined by the client by using CheckResult)

So in the above example, if the function: 
``` 
return client.GetAsync("https://google.com/news", HttpCompletionOption.ResponseContentRead); 
``` 
results in an exception, the alternate:
```
return client.GetAsync("https://www.bing.com/news", HttpCompletionOption.ResponseContentRead);
```
gets invoked.

*Checking the result*

The alternate is also invoked, if client determines that the result is not what it was expecting. This can be specified by invoking "CheckResult" on CircuitBreaker:
```
cb.CheckResult((r) => {
    return r.IsSuccessStatusCode;
});
```
In this case, if the HttpResponse does not return success status code, then CircuitBreaker will invoke the alternate. For example:
``` 
return client.GetAsync("https://google.com/news1", HttpCompletionOption.ResponseContentRead); 
``` 
resturns a 404 not found, CheckResult returns a false and the circuit breaker invokes the alternate.

*Specifying a timeout*

A timeout can be specified by the client by using "WithinTime" function. This gets applied while invoking the first choice function (InvokeAsync). For example:
```
CircuitBreaker<HttpResponseMessage> cb = new CircuitBreaker<HttpResponseMessage>(10, reducers).Alternate((token) => {
    HttpClient client = new HttpClient();
    return client.GetAsync("https://www.bing.com/news", HttpCompletionOption.ResponseContentRead);
}).CheckResult((r) => {
    return r.IsSuccessStatusCode;
}).WithinTime(new TimeSpan(0, 0, 0, 0, 200));
```
specifies that function that invokes google news (the first choice function) should return within 200 ms. If it does not return within 200 ms, circuit breaker invokes the alternate.

##### When does the circuit close?
The circuit breaker monitors the failures and holds the "last N" failures in a circular buffer. The size of this cricular buffer is determined by the parameter (bufferSz) passed to the constructor of CircuitBreaker.

After every failure (this by default, this behaviour can be overrridden see below), the circuit breaker invokes the list of reducers supplied by the client. A "reducer" function is defined by the client to enumerate the last "n" failures and determine if the circuit has to be closed.

A failure is defined by the class "Point" (A failure point) and has the following definition:
```
public class Point {
    public TimeSpan TimeTaken { get; set; }
    public Exception Fault { get; set; }        
    public bool TimedOut { get; set; }
    public FailureType FailureType { get; set; }
}
```

A reducer function can iterate through the failure points to determine if the circuit needs to be closed. Returning a true value from reducer closes the circuit.

*How frequently does the circuit breaker invoke the reducer function?*

By default, the circuit breaker invokes the reducer function upon every failure. This behaviour can be overridden by invoking the following overloaded constructor:
```
CircuitBreaker(int bufferSz, int reduceAfterNFailures, IEnumerable<Func<Point[], bool>> reducers)
```
The parameter "reduceAfterNFailures" determines after how many failures does the circuit breaker invoke the reducer function. Note this value should be less than or equal to bufferSz.

##### Reopening a closed circuit
The circuit breaker has to attempt to invoke the first choice function to check if the failures have subsided and it can be invoked in future. If such an attempt succeeds the circuit is re-opened. This mechanism is called a "probe" in Nizcita. A probe to determine if the cicruit can be re-opened.

By default, Circuit breaker attempts a probe after 5 requests in a closed state. This behaviour is meant to be overridden. It can be overridden by spefiying a probe startegy:
```
cb.ProbeStrategy((alternateCallCounter) => {
    probeStrategyCounter++;
    if (alternateCallCounter == thresholdAlternateCounter) {
        return true;
    }
    return false;
})
```
Circuit breaker keeps a count of the calls made during the closed state, and invokes probe strategy function to determine if a probe attempt should be made.

### Yet to be done

A lot, the code has not been tested thoroughly. If you find any bugs or have suggestions for improvements, please email me at: narasimha (dot) (gm) (at) gmail

Dashboard to view failures and circuit state.

Support for Alerts

For dashboard and alerts integrate with any opensource log dashboard system


----

   [sn]: <http://spokensanskrit.de/index.php?tinput=nizcita&direction=SE&script=&link=yes>

