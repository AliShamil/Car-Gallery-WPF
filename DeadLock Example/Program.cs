namespace DeadLock_Example;


//class BankAccount
//{
//    public int m_id;
//    public decimal m_balance;
//    public object m_syncLock = new object();


//    public void Transfer(BankAccount a, BankAccount b, decimal amount)
//    {
//        lock (a.m_syncLock)
//        {
//            if (a.m_balance<amount)
//                throw new Exception("Insufficient funds");

//            lock (b.m_syncLock)
//            {
//                a.m_balance -= amount;
//                b.m_balance += amount;
//                Console.WriteLine($"{a.m_balance} - {b.m_balance}");
//            }
//        }
//    }
//}


class BankAccount
{
    public int m_id;
    public decimal m_balance;
    public object m_syncLock = new object();


    public void Transfer(BankAccount a, BankAccount b, decimal amount)
    {
        MultiLockHelper.Enter(a, b);
        try
        {
            if (a.m_balance<amount)
                throw new Exception("Insufficient funds");

            a.m_balance -= amount;
            b.m_balance += amount;
            Console.WriteLine($"{a.m_balance} - {b.m_balance}");


        }
        finally
        {
            MultiLockHelper.Exit(a, b);
        }
    }
}

internal static class MultiLockHelper
{
    internal static void Enter(BankAccount a, BankAccount b)
    {
        // evvelce a sonra ise b ni qebul edirik
        if (a.m_id < b.m_id)
        {
            Monitor.Enter(a.m_syncLock);
            try
            {
                Monitor.Enter(b.m_syncLock);
            }
            catch
            {
                Monitor.Exit(a.m_syncLock); // b ugursuz olur
                throw;
            }
        }
        else
        {
            // Eks halda yerini deyishib ilk b ni qebul edirik sonra ise a ni
            Monitor.Enter(b.m_syncLock);
            try
            {
                Monitor.Enter(a.m_syncLock);
            }
            catch
            {
                Monitor.Exit(b.m_syncLock); //  a ugursuz olur
                throw;
            }
        }
    }

    internal static void Exit(BankAccount a, BankAccount b)
    {
        // Evvelce b sonra ise a ni cixardiriq
        if (a.m_id < b.m_id)
        {
            Monitor.Exit(b.m_syncLock);
            Monitor.Exit(a.m_syncLock);
        }
        else
        {
            // Evvelce a sonra ise b ni cixardiriq
            Monitor.Exit(a.m_syncLock);
            Monitor.Exit(b.m_syncLock);
        }
    }
}


// Bu klass bu meselenin generic helli ucun istifade olunur
//internal static class MultiLockHelper<T> where T : IComparable<T> 
//{
//    internal static void Enter(params T[] locks)
//    {
        // burada loclari sortd edirik
//        Array.Sort(locks);

//        int i = 0;

//        try
//        {
//            for (; i< locks.Length; i++)
//                Monitor.Enter(locks[i]);


//        }
//        catch
//        {
            // eger ugurlu olarsa monitornan cixiriq
//            for (int j = i -1; j >= 0; J--)
//                Monitor.Exit(locks[j]);
//            throw;
//        }

//    }

//    internal static void Exit<T>(params T[] locks)
//    {
//        Array.Sort(locks);
          // Ve Tersine sort ile Monitor exit edirik
//        for (int i = locks.Length - 1; i >= 0; i--)
//            Monitor.Exit(locks[i]);


//    }
//}



class Program
{

    static void Main()
    {

        #region Izah
        /*
         Burada Birinci usul ile ishlesek, 
        Eyni anda eger bir nece thread terefinden accaountlar arasinda pul transferi olsa,
        a accaountu b accauntuna 3$ , 
        b accauntu a accauntuna 5 dollar eyni anda gondermeye calishir neticede ikisi bir birini blocklayib deadlocka dushecek.
        
        Biz bunun hell yolu olaraq ozumuz MultiHelper classi yaradib gelen accauntun id lerine gore muqayise edib ishlerimizi ona gore apaririq ve ona gore bir birilerini bloklayib aciriq.

         */
        #endregion
        BankAccount a = new BankAccount { m_id = 1, m_balance = 123 };
        BankAccount b = new BankAccount { m_id = 2, m_balance = 1526 };
        var t1 = new Thread(() =>
         {

             a.Transfer(a, b, 3);
         });

        var t2 = new Thread(() =>
        {

            b.Transfer(b, a, 5);
        });
        t1.Start();
        t2.Start();
    }
}



