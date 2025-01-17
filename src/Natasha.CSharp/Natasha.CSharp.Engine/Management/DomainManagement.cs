﻿using Microsoft.Extensions.DependencyModel;
using Natasha.Framework;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static System.Runtime.Loader.AssemblyLoadContext;


public class DomainManagement
{

    public static DomainBase Default;
    public static readonly ConcurrentDictionary<string, WeakReference> Cache;
    public static Func<string, DomainBase> CreateDomain;

    static DomainManagement()
    {
        Cache = new ConcurrentDictionary<string, WeakReference>();
    }


    public static DomainBase Random
    {
        get { return Create("N" + Guid.NewGuid().ToString("N")); }
    }


    public static DomainBase Create(string key)
    {
        if (Cache.ContainsKey(key))
        {
            return (DomainBase)(Cache[key].Target);
        }
        else
        {
            Clear();
            if (CreateDomain == null)
            {
                throw new NotImplementedException("请检查组件是否被注册! 如果使用 Natasha.CSharp.All 请在初始化使用: NatashaInitializer.InitializeAndPreheating 方法进行初始化.");
            }
            var domain = CreateDomain(key);
            Add(key, domain);
            return domain;
        }
    }


    public static void Clear()
    {
        foreach (var item in Cache)
        {
            if (!item.Value.IsAlive)
            {
                Cache.Remove(item.Key);
            }
        }
    }


#if NETCOREAPP3_0_OR_GREATER
        public static ContextualReflectionScope Lock(string key)
        {
            if (Cache.ContainsKey(key))
            {
                return ((DomainBase)(Cache[key].Target)).EnterContextualReflection();
            }
            return Default.EnterContextualReflection();
        }
        public static ContextualReflectionScope Lock(DomainBase domain)
        {
            return domain.EnterContextualReflection();
        }
        public static ContextualReflectionScope CreateAndLock(string key)
        {
            return Lock(Create(key));
        }
        public static DomainBase CurrentDomain
        {
            get
            {
                return CurrentContextualReflectionContext==default?
                    (DomainBase)Default :
                    (DomainBase)CurrentContextualReflectionContext;
            }
        }
#endif


    public static void Add(string key, DomainBase domain)
    {
        if (Cache.ContainsKey(key))
        {
            if (!Cache[key].IsAlive)
            {
                Cache[key] = new WeakReference(domain);
            }
        }
        else
        {
            Cache[key] = new WeakReference(domain, trackResurrection: true);
        }
    }


    public static WeakReference Remove(string key)
    {
        if (Cache.ContainsKey(key))
        {
            var result = Cache.Remove(key);
            if (result != default)
            {
                ((DomainBase)(result.Target)).Dispose();
            }
            return result;
        }

        throw new System.Exception($"Can't find key : {key}!");
    }


    public static bool IsDeleted(string key)
    {
        if (Cache.ContainsKey(key))
        {
            return !Cache[key].IsAlive;
        }
        return true;
    }


    public static DomainBase Get(string key)
    {
        if (Cache.ContainsKey(key))
        {
            return (DomainBase)Cache[key].Target;
        }
        return null;
    }


    public static int Count(string key)
    {
        return ((DomainBase)(Cache[key].Target)).Count;
    }
}
