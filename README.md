NetDist
=======

NetDist (short for ".Net Distribution") is a ready-to-use task or job distribution system for .Net and C#.
The main idea behind it is, that there are job-generators (handlers) which generate jobs (for example from a database) and those jobs are processed by clients which then send the result back to the server who then processes the result.
The handlers and the jobs can be added / modified on runtime without restarting the server or any client.

Here are some of the features of it:
* Runtime addition / updating of packages (eg. server logic)
* Runtime addition / updating of tasks with just C# files! (eg. client logic)
* Add as many clients as you want
* All-in-one solution (server + server administrator + client)
* Easily extensible (like other communications, currently only Web Api is implemented)
* Idle time (timespan were a handler is not sending any jobs)
* Allow / disallow specific clients to run a job

### Build Status
|Repo|Appveyor|
|:---|:------------------|
|[NetDist](https://github.com/Roemer/NetDist)|[![Build status](https://ci.appveyor.com/api/projects/status/pgy2svo2oaoqf1td?svg=true)](https://ci.appveyor.com/project/RomanBaeriswyl/netdist)|

### Further reading
Head over to the [wiki](https://github.com/Roemer/NetDist/wiki) for more information or just read the [simple usage guide](https://github.com/Roemer/NetDist/wiki/Simple-Usage) to get a quick introduction on how to use the whole system.
