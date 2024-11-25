using System.Reflection;
using System.Text;
using MongoDB.Bson.Serialization;

namespace Shared;

/// <summary>
/// Crates MongoDb mappings
/// </summary>
public static class MongoDbAutomapper
{
    public static void Automap(
        string folder,
        string[]? exclusionPatterns)
    {
        // get all the types inside all the assemblyes in a aspcified folder

        // commands, events, notification, Id
        var autoMapTypes = new List<Type>();
        // snapshots
        var snapshottableTypes = new List<Type>();

        var patterns = exclusionPatterns ?? Array.Empty<string>();

        // list of assemblies that cause problems or throw System.BadImageFormatException
        var files = Directory.EnumerateFiles(folder)
            .Where(i => !patterns.Any(p => i.Contains(p, StringComparison.InvariantCultureIgnoreCase)))
            .ToList();
        foreach (var fileName in files)
        {
            //provare a caricare dinamicamente un assembly
            var extension = Path.GetExtension(fileName);
            if (extension == null
                ||
                    !extension.EndsWith("dll", StringComparison.InvariantCultureIgnoreCase)
                    && !extension.EndsWith("exe", StringComparison.InvariantCultureIgnoreCase)
                    )
            {
                continue;
            }

            // try to load the assembly
            Assembly? dynamicAsm = null;
            try
            {
                var asmName = AssemblyName.GetAssemblyName(fileName);
                dynamicAsm = Assembly.Load(asmName);
            }
            catch (Exception ex)
            {
                // todo: maybe log assembly loading errors, can lead to automapping errors
                Console.WriteLine(ex.Message);
            }

            // if the assembly was loaded automap the elements
            if (dynamicAsm != null)
            {
                Type[] dynamicAsmTypes;
                try
                {
                    dynamicAsmTypes = dynamicAsm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var exSub in ex.LoaderExceptions)
                    {
                        if (exSub != null)
                        {
                            sb.AppendLine(exSub.Message);
                            if (exSub is FileNotFoundException exFileNotFound
                                && !string.IsNullOrEmpty(exFileNotFound.FusionLog))
                            {
                                sb.AppendLine("Fusion Log:");
                                sb.AppendLine(exFileNotFound.FusionLog);
                            }
                            sb.AppendLine();
                        }
                    }
                    string errorMessage = sb.ToString();
                    //Display or log the error based on your application.
                    throw new Exception($"MongoDB Automapper - ReflectionTypeLoadException Getting Types from file: {fileName}\n\n{errorMessage}", ex);
                }
                catch (Exception ex)
                {
                    throw new Exception($"MongoDB Automapper - Error Getting Types from file: {fileName}", ex);
                }

                // automap: Events, Commands, Notification
                IEnumerable<Type> allAssemblyTypes = dynamicAsmTypes
                    .Where(x => x.IsClass
                        && !x.IsAbstract
                        && !x.ContainsGenericParameters
                        && typeof(IMessage).IsAssignableFrom(x));
                autoMapTypes.AddRange(allAssemblyTypes);

                /* add snapshot is needed

                // register all the snapshottable types
                // tutti gli aggregati che derivano da snapshottable il cui base type
                // prendo il tipo assegnato alla proprietà State (lo ricavo dalla lista degli argomento del
                // base type generico) per limitare il numero di classi mappate
                // posso usare strategie differenti
                IEnumerable<Type> allSnapshottableAggregates = dynamicAsmTypes
                    .Where(x => x.IsClass
                        && !x.IsAbstract
                        && x.GetCustomAttributes(typeof(IsSnapshottableAttribute), false).Length > 0);
                foreach (var aggregate in allSnapshottableAggregates)
                {
                    // get the second argument (the snapshot)
                    var genericArguments = aggregate.BaseType?.GetGenericArguments();
                    if (genericArguments?.Length > 1)
                    {
                        var stateType = genericArguments.Last();
                        var snapshot = typeof(AggregateSnapshot<>).MakeGenericType(stateType);
                        snapshottableTypes.Add(snapshot);
                    }
                }
                */
            }
        }

        // automapping domain events, it's needed to help MongoDb engine to retrieve the data
        foreach (var type in autoMapTypes)
        {
            // this one generates lots of unneeded mapping (event for the base classes types, we just want the end types of the hierarchy right now)
            //BsonClassMap.LookupClassMap(type);
            var cm = new CommandsEventsNotificationsIdentitiesClassMap(type);
            cm.RegisterClassMapIfNotRegistered();

        }

        /*
        // automapping snapshots
        foreach (var type in snapshottableTypes)
        {
            var cm = new AggregateSnaphotClassMap(type);
            cm.RegisterClassMapIfNotRegistered();
        }
        */

        CheckForMappingProblems();
    }

    private static void CheckForMappingProblems()
    {
        // look for ambigous registrations
        var mappings = BsonClassMap.GetRegisteredClassMaps();
        var ambiguous = mappings.GroupBy(cm => cm.Discriminator).Where(grp => grp.Count() > 1).ToList();
        if (ambiguous.Count > 0)
        {
            var errorMsgSb = new StringBuilder();
            // let's get some more information on ambiguous mappings
            foreach (var group in ambiguous)
            {
                errorMsgSb.Append("Discriminator: ").AppendLine(group.Key);
                var collidingMappings = mappings.Where(m => m.Discriminator == group.Key).ToList();
                foreach (var m in collidingMappings)
                {
                    errorMsgSb.AppendLine(m.ClassType.FullName);
                }
            }

            throw new ApplicationException("Ambiguous elements in MongoDB mappings: \n\r" + errorMsgSb.ToString());
        }
    }
}