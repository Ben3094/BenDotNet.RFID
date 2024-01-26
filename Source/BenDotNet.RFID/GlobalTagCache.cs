﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace BenDotNet.RFID
{
    public static class GlobalTagCache
    {
        public static Dictionary<Type, Type[]> AvailableTagTypes = new Dictionary<Type, Type[]>();
        public static void UpdateAvailableTagTypes()
        {
            UpdateAvailableTagTypes(typeof(Tag));
        }
        public static Type[] UpdateAvailableTagTypes(Type baseTagType)
        {
            IEnumerable<Type> availableTagTypes = new List<Type>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                //try { ((List<Type>)availableTagTypes).AddRange(assembly.GetTypes().Where(t => (t != baseTagType) && baseTagType.IsAssignableFrom(t) && !t.IsAbstract && t.IsPublic)); }
                try { ((List<Type>)availableTagTypes).AddRange(assembly.GetTypes().Where(t => baseTagType.IsAssignableFrom(t) && !t.IsAbstract && t.IsPublic)); }
                catch (ReflectionTypeLoadException) { }
            availableTagTypes = availableTagTypes.Distinct();
            availableTagTypes = availableTagTypes.OrderByDescending(type => Helpers.ReccursiveSearchForBaseType(type, baseTagType));
            availableTagTypes = availableTagTypes.ToArray();
            AvailableTagTypes.Add(baseTagType, ((Type[])availableTagTypes));
            return ((Type[])availableTagTypes);
        }

        public static readonly ObservableCollection<Tag> DetectedTags = new ObservableCollection<Tag>();

        public static Tag NotifyDetection(byte[] uid, Type baseTagType, DetectionSource newDetectionSource)
        {
            if (!baseTagType.IsSubclassOf(typeof(Tag)) && (baseTagType != typeof(Tag))) throw new ArgumentException("Not a type of tag");

            //Search for corresponding tag
            Tag tag = null;
            try
            {
                tag = DetectedTags.First(detectedTag => detectedTag.UID.SequenceEqual(uid));

                //TODO: Integrate multi-type tags
                //if (!(tag.GetType().IsSubclassOf(baseTagType)) && (!baseTagType.Equals(tag)))
                //{
                //    DetectedTags.
                //}

                //Delete old detection source from the same antenna
                try
                {
                    DetectionSource previousDetectionSource = tag.DetectionSources.First(detectionSource => detectionSource.Antenna == newDetectionSource.Antenna);
                    if (previousDetectionSource.Time < newDetectionSource.Time) //Check time order
                    {
                        tag.DetectionSources.Remove(previousDetectionSource);
                        AddAndSortDetectionSource(ref tag, newDetectionSource);
                    }
                    //Else ignore the current detection as it is anterior of the already present one
                }
                catch (InvalidOperationException)
                {
                    AddAndSortDetectionSource(ref tag, newDetectionSource);
                }
            }
            catch (InvalidOperationException)
            {
                //If it is the first time it is detected,...
                //...search for tag types that were already searched,...
                Type[] correpondingTagTypes = null;
                try { correpondingTagTypes = AvailableTagTypes[baseTagType]; }
                //...if not present, do search.
                catch (KeyNotFoundException)
                { correpondingTagTypes = UpdateAvailableTagTypes(baseTagType); }
                //TODO: Not Resilient enough, it do not search for newly introduced types

                //Search for tag type that can initialize (UID correspond)
                //TODO: Place Subclass first to be initiated first
                foreach (Type correspondingTagType in correpondingTagTypes)
                {
                    try
                    {
                        tag = (Tag)Activator.CreateInstance(correspondingTagType, uid, newDetectionSource);

                        //TODO: Do not break, create multitypetag if found several corresponding type, equals in derived branch
                        break;
                    }
                    catch (Exception e) { }
                }
                if (tag == null) throw new NotImplementedException("Tag type that support UID not found");

                DetectedTags.Add(tag);
            }
            //Update original antenna port with detected tag
            newDetectionSource.Antenna.ConnectedTags.Add(tag);

            return tag;
        }

        /// <summary>
        /// Add detection source to the tag, sorted by RSSI
        /// </summary>
        /// <param name="detectedTag"></param>
        /// <param name="newDetectionSource"></param>
        private static void AddAndSortDetectionSource(ref Tag detectedTag, DetectionSource newDetectionSource)
        {
            int newDetectionSourceSortingPointer = 0;
            DetectionSource previousDetectionSource = null;
            try
            {
                previousDetectionSource = detectedTag.DetectionSources.First(detectionSource => detectionSource.RSSI <= newDetectionSource.RSSI);
            }
            catch (InvalidOperationException) { }
            if (previousDetectionSource != null)
                newDetectionSourceSortingPointer = detectedTag.DetectionSources.IndexOf(previousDetectionSource) - 1;
            if (newDetectionSourceSortingPointer < 0)
                newDetectionSourceSortingPointer = 0;
            detectedTag.DetectionSources.Insert(newDetectionSourceSortingPointer, newDetectionSource);
        }
    }
}
