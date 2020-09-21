open Hopac
open System
open System.IO
open System.Collections.Concurrent
open System.Threading
open System.Threading.Tasks
open System.Diagnostics


type Message =
    | FromWest of int
    | FromNorth of int  

    
type inputLists = 
    {
    string1: string[];string2: string[];    sent1: int[][];    sent2: int[][];    d1:int;    d2:int;    t:int
    }
type inputStrings = 
    {
    string1: string;    string2: string;    sent1: int[];    sent2: int[];
    }
type agent =
    {
    string1: string;    string2: string;    sent1: int[];    sent2: int[];    agents:MailboxProcessor<Message>[,];    id1:int;    id2:int;     tt:int
    }
type jobs =
    {
    string1: string;    string2: string;    sent1: int[];    sent2: int[];    chan:Ch<Message>[,];    id1:int;    id2:int;     tt:int
    }
    
let result = TaskCompletionSource<int*int> ()
let res = TaskCompletionSource<Unit> ()


// ------------diag_loop-------------------------
let diag (z1:string) (z2:string) (s1:int[]) (s2:int[]) : int= 
    let n1, n2 = z1.Length, z2.Length
    let d0 = Array.create n1 0
    let d1 = Array.create n1 0
    let d2 = Array.create n1 0
    let mutable r = 0
    //for k = 2 to n1+n2-2 do
    let rec loop k (d0: int[]) (d1: int[]) (d2: int[]) =   
        let lo = if k < n2 then 1 else k-n2+1 
        let hi = if k < n1 then k-1 else n1-1 
        if k = 2 then d1.[1]<-s1.[1] else ()
        for i = lo to hi do
            if z1.[i] = z2.[k-i] 
            then 
                match i with 
                | 1 -> d2.[i] <- s2.[k-2] + 1 
                | h when h=hi -> if k<n1+1  then d2.[i] <- s1.[k-2]+1 else  d2.[i] <- d0.[i-1] + 1                                         
                | _ -> d2.[i] <- d0.[i-1] + 1 
            else 
                match i with 
                | 1 -> d2.[i] <- max s2.[k-1] d1.[1] 
                | h when h=hi -> if k<=n1 then d2.[i] <- max d1.[i-1] s1.[k-1] else d2.[i] <- max d1.[i-1] d1.[i]
                | _ -> d2.[i] <- max d1.[i-1] d1.[i]

        if k=n1 then s2.[0]<-s1.[n1-1] else()
        if k=n2 then s1.[0]<-s2.[n2-1] else()
        if k >n1 then s2.[k-n1+1]<-d2.[hi] else ()
        if k >n2 then s1.[k-n2+1]<-d2.[lo] else ()
        if k < n1+n2-2 then loop (k+1) d1 d2 d0  else  r <- d2.[n1-1]      
    loop 2 d0 d1 d2
    r

let split' n div =
    let q, r = n/div, n%div
    let l = List.replicate r (q+1) @ if q = 0 then [] else List.replicate (div-r) q
    let b = l |> List.scan (fun s n -> s+n) 0
    let z = Seq.zip b l
    Seq.toArray z

    
let split (str1:string)(str2:string)(div1:int) (div2:int)(t:int):inputLists =
    let n1, n2 = str1.Length, str2.Length 
     
    let z1, z2 = split' n1 div1, split' n2 div2
    let str1', str2' = "?" + str1, "?" + str2
    let mutable str1's,str2's = Array.zeroCreate<string> 0,Array.zeroCreate<string> 0
    let mutable sent1's,sent2's = Array.zeroCreate<int[]> 0,Array.zeroCreate<int[]> 0
    str1's <- z1 |> Array.map (fun (b, l) -> str1'.Substring (b, l+1))
    str2's <- z2 |> Array.map (fun (b, l) -> str2'.Substring (b, l+1))
    sent1's <- z1 |> Array.map (fun (_, l) -> Array.zeroCreate<int> (l+1))
    sent2's <- z2 |> Array.map (fun (_, l) -> Array.zeroCreate<int> (l+1))
    {string1=str1's; string2=str2's;sent1= sent1's; sent2=sent2's;d1=div1;d2=div2;t= t}

let agent (agentIn:agent) =
    let string1,string2,sentinel1,sentinel2,agents,i1, i2,tt=agentIn.string1,agentIn.string2,agentIn.sent1,agentIn.sent2,agentIn.agents ,agentIn.id1,agentIn.id2, agentIn.tt     
    MailboxProcessor<Message>.Start <| fun inbox ->
        let b1 = Array2D.length1 agents
        let b2 = Array2D.length2 agents
               
        async {
            let mutable n = 0
            let mutable w = 0
            let! m = inbox.Receive () 
            match m with
            | FromNorth t -> n <- t 
            | FromWest t -> w <- t

            let! m' = inbox.Receive () 
            match m' with
            | FromNorth t -> n <- t
            | FromWest t -> w <- t

            let r= diag string1 string2 sentinel1 sentinel2//Partial LCS
            if tt=1 then 
                Console.WriteLine ("{0} {1} {2}", i1, i2, r)
            else ()

            if i1 = b1-1 && i2 = b2-1 then //done
                if tt = 0 then 
                    Console.WriteLine ("{0} {1} {2}", i1, i2, r)
                else ()
                result.SetResult (n+1, w+1)              
            else
                if i1 < b1-1 then agents.[i1+1, i2].Post (FromNorth (n+1))
                if i2 < b2-1 then agents.[i1, i2+1].Post (FromWest (w+1))            
        }
        
let agentsCreater (inputS:inputLists) :MailboxProcessor<Message>[,]=
    let agents = Array2D.zeroCreate<MailboxProcessor<Message>> inputS.d1 inputS.d2 
    for i1 = 0 to inputS.d1-1 do
        for i2 = 0 to inputS.d2-1 do
            agents.[i1, i2] <- agent {string1=inputS.string1.[i1];string2= inputS.string2.[i2];sent1= inputS.sent1.[i1];sent2= inputS.sent2.[i2];agents= agents;id1= i1;id2= i2;tt= inputS.t }
    agents
    
let post (agentsArray:MailboxProcessor<Message>[,])=    
    for i2 = 1 to  Array2D.length2 agentsArray-1 do  // up to down
        agentsArray.[0, i2].Post (FromNorth 0)   
    for i1 = 1 to Array2D.length1 agentsArray-1 do //left to right
        agentsArray.[i1, 0].Post (FromWest 0) 
    agentsArray.[0, 0].Post (FromNorth 0)
    agentsArray.[0, 0].Post (FromWest 0) 
    result.Task.Result
    


// Job<unit>

let JobSmall (a_job:jobs)  = 
    let ajob a_job =job {
        let mutable n = 0
        let mutable w = 0
        let string1,string2,sentinel1,sentinel2,channels,i1, i2,tt=a_job.string1,a_job.string2,a_job.sent1,a_job.sent2,a_job.chan,a_job.id1,a_job.id2, a_job.tt  
        let b1 = Array2D.length1 channels
        let b2 = Array2D.length2 channels
        let! m = Ch.take channels.[i1,i2]
        match m with
        | FromNorth t -> n <- t 
        | FromWest t -> w <- t

        let! m' = Ch.take channels.[i1,i2] 
        match m' with
        | FromNorth t -> n <- t
        | FromWest t -> w <- t


        let r= diag string1 string2 sentinel1 sentinel2//LCS

        if tt=1 then 
            Console.WriteLine ("{0} {1} {2}", n, w, r)
        else ()

        if n = b1-1 && w = b2-1 then //done
            if tt = 0 then 
                Console.WriteLine ("{0} {1} {2}", n, w, r)

            else ()
        else
            if n < b1-1 then do! Ch.send channels.[i1+1,i2] (FromNorth(n+1)) else ()// Post to South
            if w < b2-1 then do! Ch.send channels.[i1,i2+1] (FromWest(w+1)) else() // Post to East 

    }
    start (ajob a_job)
    

let jobsCreater (inputS:inputLists) =job{    
    let chansArray = Array2D.zeroCreate<Ch<Message>> inputS.d1 inputS.d2
    let jobsArray = Array2D.zeroCreate<Unit> inputS.d1 inputS.d2
    for i1 = 0 to inputS.d1-1 do
        for i2 = 0 to inputS.d2-1 do
            chansArray.[i1,i2]<- Ch<Message>()
            jobsArray.[i1,i2]<- JobSmall {string1=inputS.string1.[i1];string2= inputS.string2.[i2];sent1= inputS.sent1.[i1];sent2= inputS.sent2.[i2];chan= chansArray;id1= i1;id2= i2;tt= inputS.t }

    for i2 = 1 to inputS.d2-1 do 
        do! Ch.send  chansArray.[0, i2] (FromNorth 0)
    for i1 = 1 to inputS.d1-1 do 
        do! Ch.send chansArray.[i1, 0] (FromWest 0)
    do! Ch.send chansArray.[0, 0] (FromNorth 0)
    do! Ch.send chansArray.[0, 0] (FromWest 0)    
    }
    

[<EntryPoint>]
let main args =
    try
        let path1 = args.[0].Remove(0,4)
        let path2 = args.[1].Remove(0,4)
        let s1 = System.IO.File.ReadAllText (path1)
        let s2 = System.IO.File.ReadAllText (path2)
        let s3 = args.[2]
        let found = s3.IndexOf(":")
        let s3new = s3.Substring(found+1)
        let numbers = s3new.Split [|','|]
        match args.[2].Substring(0,4) with
        | "/SEQ" -> 
            let r = split s1 s2 1 1 0 |> agentsCreater |> post
            ()
        | "/ACT" -> 
            let d1 = Convert.ToInt32(numbers.[0])
            let d2 = Convert.ToInt32(numbers.[1])
            let t = Convert.ToInt32(numbers.[2])
            let r = split s1 s2 d1 d2 t |> agentsCreater |> post
            ()                
        | "/CSP" -> 
            let d1 = Convert.ToInt32(numbers.[0])
            let d2 = Convert.ToInt32(numbers.[1])
            let t = Convert.ToInt32(numbers.[2])
            split s1 s2 d1 d2 t |> jobsCreater|>run
            res.Task.Result
            ()
        | _ -> 
            Console.WriteLine ("Invalid command")
    with
        | e -> 
            Console.WriteLine ("Error message: {0} \n From: {1}", e.Message, e.Source)
    0 