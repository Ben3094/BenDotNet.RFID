﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Timers;

namespace BenDotNet.RFID
{
    public enum ReaderStatus { Unknown, Error, Disconnected, Connected } 

    public abstract class Reader : INotifyPropertyChanged
    {
        static Reader() { UpdateAvailableReaderTypes(); }
        public Reader()
        {
            foreach (AntennaPort antennaPort in this.AntennaPorts)
                antennaPort.ConnectedTags.CollectionChanged += ConnectedTags_CollectionChanged;
        }

        public ReaderStatus status;
        public ReaderStatus Status
        {
            get { return this.status; }
            protected set
            {
                this.status = value;
                OnPropertyChanged();
            }
        }

        private void ConnectedTags_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            foreach (Tag addedTag in e.NewItems)
                this.ConnectedTags.Add(addedTag);
            foreach (Tag removedTag in e.OldItems)
                this.ConnectedTags.Remove(removedTag);
        }
        public readonly ObservableCollection<Tag> ConnectedTags = new ObservableCollection<Tag>();

        public object OriginalReader;

        public void Connect()
        {
            this.ConnectionMethod();
            this.Status = ReaderStatus.Connected;
        }
        protected abstract void ConnectionMethod();

        public void Disconnect()
        {
            this.DisconnectionMethod();
            this.Status = ReaderStatus.Disconnected;
        }
        protected abstract void DisconnectionMethod();

        public abstract Reply Execute(Tag targetTag, Command command);

        public abstract IEnumerable<AntennaPort> AntennaPorts { get; }

        public const ushort DEFAULT_INVENTORY_INTERVAL_ms = 500;
        public static TimeSpan DEFAULT_INVENTORY_INTERVAL = TimeSpan.FromMilliseconds(DEFAULT_INVENTORY_INTERVAL_ms);
        protected Timer AutoInventoryTimer = new Timer() { AutoReset = true };
        protected TimeSpan autoInventoryDelay = DEFAULT_INVENTORY_DELAY;
        private void AutoInventoryTimer_Tick(object sender, ElapsedEventArgs e) { this.Inventory(this.autoInventoryDelay); }
        public virtual void StartContinuousInventory(TimeSpan interval, TimeSpan delay)
        {
            this.autoInventoryDelay = delay;
            this.AutoInventoryTimer.Elapsed += AutoInventoryTimer_Tick;
            this.AutoInventoryTimer.Interval = interval.TotalMilliseconds;
            this.AutoInventoryTimer.Start();
        }
        public virtual void StartContinuousInventory() { this.StartContinuousInventory(DEFAULT_INVENTORY_INTERVAL, DEFAULT_INVENTORY_DELAY); }
        public virtual void StopContinuousInventory() 
        {
            this.AutoInventoryTimer.Elapsed -= AutoInventoryTimer_Tick;
            this.AutoInventoryTimer.Stop();
        }
        public const ushort DEFAULT_INVENTORY_DELAY_ms = 500;
        public static TimeSpan DEFAULT_INVENTORY_DELAY = new TimeSpan(0, 0, 0, 0, DEFAULT_INVENTORY_DELAY_ms);
        public abstract IEnumerable<Tag> Inventory(TimeSpan delay);
        public IEnumerable<Tag> Inventory() { return this.Inventory(DEFAULT_INVENTORY_DELAY); }
        public virtual Tag Detect(ref Tag tag, TimeSpan delay)
        {
            Tag targetTag = tag;
            try { return this.Inventory(delay).First(detectedTag => detectedTag == targetTag); }
            catch (InvalidOperationException) { return null; }
        }
        public Tag Detect(ref Tag tag)
        {
            return this.Detect(ref tag, DEFAULT_INVENTORY_DELAY);
        }
        
        /// <summary>
        /// Frequency (in Hertz) used by the reader to operate
        /// </summary>
        /// <remarks>
        /// 0 for automatic
        /// Always use the closest value in the allowed frequencies range
        /// </remarks>
        public abstract float Frequency { get; set; }
        public abstract float MaxAllowedFrequency { get; }
        public abstract float MinAllowedFrequency { get; }

        /// <summary>
        /// Power (in decibel-milliwatt) used by the reader to operate
        /// </summary>
        /// <remarks>
        /// 0 for automatic
        /// Always use the closest value in the allowed power range
        /// </remarks>
        public abstract float Power { get; set; }
        public abstract float MaxAllowedPower { get; }

        #region Properties changes event handler
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Auto-detection
        public static ReadOnlyCollection<Type> AvailableReaderTypes { get => availableReaderTypes; }
        internal static ReadOnlyCollection<Type> availableReaderTypes;
        public static void UpdateAvailableReaderTypes()
        {
            Type readerType = typeof(Reader);
            IEnumerable<Type> availableReaderTypes = new List<Type>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                try { ((List<Type>)availableReaderTypes).AddRange(assembly.GetTypes().Where(t => (t != readerType) && readerType.IsAssignableFrom(t) && !t.IsAbstract && t.IsPublic)); }
                catch (ReflectionTypeLoadException) { }
            availableReaderTypes = availableReaderTypes.Distinct();
            availableReaderTypes = availableReaderTypes.OrderByDescending(type => Helpers.ReccursiveSearchForBaseType(type, readerType));
            Reader.availableReaderTypes = availableReaderTypes.ToList().AsReadOnly();
        }
        #endregion
    }
}
