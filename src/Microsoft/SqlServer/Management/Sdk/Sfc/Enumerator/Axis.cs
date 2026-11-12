// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    internal class Axis : AstNode {
        private static readonly String[] str = {
            "Ancestor",
            "AncestorOrSelf",
            "Attribute",
            "Child",
            "Descendant",
            "DescendantOrSelf",
            "Following",
            "FollowingSibling",
            "Namespace",
            "Parent",
            "Preceding",
            "PrecedingSibling",
            "Self"
        };

        private AxisType _axistype;
        private AstNode _input;
        private String _urn;
        private String _prefix;
        private String _name;
        private XPathNodeType _nodetype;

        internal enum AxisType {
            Ancestor=0,
            AncestorOrSelf,
            Attribute,
            Child,
            Descendant,
            DescendantOrSelf,
            Following,
            FollowingSibling,
            Namespace,
            Parent,
            Preceding,
            PrecedingSibling,
            Self,
            None
        };

        // constructor
        internal Axis(
                     AxisType axistype,
                     AstNode input,
                     String urn,
                     String prefix,
                     String name,
                     XPathNodeType nodetype) {
            _axistype = axistype;
            _input = input;
            _urn = urn;
            _prefix = prefix;
            _name = name;
            _nodetype = nodetype;
        }

        // constructor
        internal Axis(AxisType axistype, AstNode input) {
            _axistype = axistype;
            _input = input;
            _urn = String.Empty;
            _prefix = String.Empty;
            _name = String.Empty;
            _nodetype =  XPathNodeType.All;
        }

        internal override QueryType TypeOfAst {
            get {return  QueryType.Axis;}
        }

        internal override RType ReturnType {
            get {return RType.NodeSet;}
        }

        internal AstNode Input {
            get {return _input;}
        }

        internal String Name {
            get {return _name;}
        }

        internal AxisType TypeOfAxis {
            get {return _axistype;}
        }

        internal override double DefaultPriority {
            get {
                if (_input != null)
                {
                    return 0.5;
                }

                if (_axistype == AxisType.Child|| _axistype == AxisType.Attribute) {

                    if (_name != null && _name.Length != 0)
                    {
                        return 0;
                    }

                    if (_prefix != null && _prefix.Length != 0)
                    {
                        return -0.25;
                    }

                    return -0.5;
                }
                return 0.5;
            }
        }
    }
}
