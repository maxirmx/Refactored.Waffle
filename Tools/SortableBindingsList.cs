// Copyright (C) 2024 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of OiltrackGateway applcation
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
// 1. Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
// TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
// PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDERS OR CONTRIBUTORS
// BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

using System.ComponentModel;
using System.Linq.Dynamic.Core;

namespace Refactored.Waffle.Tools;

public sealed class SortableBindingList<TEntity> : BindingList<TEntity>, IBindingListView
{
    private ListSortDescriptionCollection _sortDescriptions;
    private bool _isSorted;
    private PropertyDescriptor? _sortProperty;
    private ListSortDirection _sortDirection;

    public SortableBindingList(ListSortDescriptionCollection sortDescriptions)
    {
        _sortDescriptions = sortDescriptions;
        ApplySort();
    }

    protected override bool SupportsSortingCore => true;

    protected override bool IsSortedCore => _isSorted;

    protected override ListSortDirection SortDirectionCore => _sortDirection;

    protected override PropertyDescriptor? SortPropertyCore => _sortProperty;

    public ListSortDescriptionCollection SortDescriptions => _sortDescriptions;

    public string? Filter { get; set; }

    public bool SupportsAdvancedSorting => true;

    public bool SupportsFiltering => true;

    protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
    {
        _sortProperty = prop;
        _sortDirection = direction;

        var items = Items as List<TEntity>;
        if (items == null)
        {
            return;
        }

        var query = direction == ListSortDirection.Ascending
            ? items.AsQueryable().OrderBy(prop.Name)
            : items.AsQueryable().OrderBy($"{prop.Name} descending");

        var sortedItems = query.ToList();
        items.Clear();
        foreach (var item in sortedItems)
        {
            items.Add(item);
        }

        _isSorted = true;
        OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
    }

    public void ApplySort()
    {
        if (_sortDescriptions == null || _sortDescriptions.Count == 0 || Items is not List<TEntity> items)
        {
            return;
        }

        var query = items.AsQueryable();
        foreach (ListSortDescription sortDescription in _sortDescriptions)
        {
            var property = sortDescription.PropertyDescriptor;
            var direction = sortDescription.SortDirection == ListSortDirection.Ascending ? "ascending" : "descending";
            query = query.OrderBy($"{property?.Name} {direction}");
        }

        var sortedItems = query.ToList();
        items.Clear();
        foreach (var item in sortedItems)
        {
            items.Add(item);
        }

        _isSorted = true;
        OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
    }
    public void ApplySort(ListSortDescriptionCollection sorts)
    {
        _sortDescriptions = sorts;
        ApplySort();
    }

    public void RemoveFilter()
    {
        Filter = null;
    }
}
