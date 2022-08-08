using System;
using System.Collections.Generic;
using System.Linq;

namespace Milki.OsuPlayer.Presentation.ObjectModel
{
    public class OrderedModel<T> : OrderedModel, IEquatable<OrderedModel<T>>, IOrderedModel
    {
        public OrderedModel()
        {
        }

        public OrderedModel(int index, T model)
        {
            Index = index;
            Model = model;
        }

        public new T Model
        {
            get => (T)base.Model;
            set => base.Model = value;
        }

        public bool Equals(OrderedModel<T> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualityComparer<T>.Default.Equals(Model, other.Model);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OrderedModel<T>)obj);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<T>.Default.GetHashCode(Model);
        }

        public static bool operator ==(OrderedModel<T> left, OrderedModel<T> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(OrderedModel<T> left, OrderedModel<T> right)
        {
            return !Equals(left, right);
        }

        public static implicit operator T(OrderedModel<T> model)
        {
            return model.Model;
        }

        object IOrderedModel.Model { get => Model; set => Model = (T)value; }
    }

    public class OrderedModel : IEquatable<OrderedModel>, IOrderedModel
    {
        public int Index { get; set; }
        public object Model { get; set; }

        public bool Equals(OrderedModel other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Model, other.Model);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OrderedModel)obj);
        }

        public override int GetHashCode()
        {
            return (Model != null ? Model.GetHashCode() : 0);
        }

        public static bool operator ==(OrderedModel left, OrderedModel right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(OrderedModel left, OrderedModel right)
        {
            return !Equals(left, right);
        }
    }

    public static class OrderedModelExtension
    {
        public static IEnumerable<OrderedModel<T>> AsOrdered<T>(this IEnumerable<T> orderedModel)
        {
            return orderedModel.Select((k, i) => new OrderedModel<T>(i, k));
        }
    }

    public interface IOrderedModel
    {
        int Index { get; set; }
        object Model { get; set; }
    }
}