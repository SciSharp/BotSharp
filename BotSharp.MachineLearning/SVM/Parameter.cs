/*
 * SVM.NET Library
 * Copyright (C) 2008 Matthew Johnson
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */


using System;
using System.Linq;
using System.Collections.Generic;

namespace SVM.BotSharp.MachineLearning
{
    /// <summary>
    /// Contains all of the types of SVM this library can model.
    /// </summary>
    public enum SvmType { 
        /// <summary>
        /// C-SVC.
        /// </summary>
        C_SVC, 
        /// <summary>
        /// nu-SVC.
        /// </summary>
        NU_SVC, 
        /// <summary>
        /// one-class SVM
        /// </summary>
        ONE_CLASS, 
        /// <summary>
        /// epsilon-SVR
        /// </summary>
        EPSILON_SVR, 
        /// <summary>
        /// nu-SVR
        /// </summary>
        NU_SVR 
    };
    /// <summary>
    /// Contains the various kernel types this library can use.
    /// </summary>
    public enum KernelType { 
        /// <summary>
        /// Linear: u'*v
        /// </summary>
        LINEAR, 
        /// <summary>
        /// Polynomial: (gamma*u'*v + coef0)^degree
        /// </summary>
        POLY, 
        /// <summary>
        /// Radial basis function: exp(-gamma*|u-v|^2)
        /// </summary>
        RBF, 
        /// <summary>
        /// Sigmoid: tanh(gamma*u'*v + coef0)
        /// </summary>
        SIGMOID,
        /// <summary>
        /// Precomputed kernel
        /// </summary>
        PRECOMPUTED,
    };

    /// <summary>
    /// This class contains the various parameters which can affect the way in which an SVM
    /// is learned.  Unless you know what you are doing, chances are you are best off using
    /// the default values.
    /// </summary>
	[Serializable]
	public class Parameter : ICloneable
	{
        /// <summary>
        /// Default Constructor.  Gives good default values to all parameters.
        /// </summary>
        public Parameter()
        {
            SvmType = SvmType.C_SVC;
            KernelType = KernelType.RBF;
            Degree = 3;
            Gamma = 0; // 1/k
            Coefficient0 = 0;
            Nu = 0.5;
            CacheSize = 40;
            C = 1;
            EPS = 1e-3;
            P = 0.1;
            Shrinking = true;
            Probability = false;
            Weights = new Dictionary<int, double>();
        }

        /// <summary>
        /// Type of SVM (default C-SVC)
        /// </summary>
        public SvmType SvmType{get;set;}

        /// <summary>
        /// Type of kernel function (default Polynomial)
        /// </summary>
        public KernelType KernelType{get;set;}

        /// <summary>
        /// Degree in kernel function (default 3).
        /// </summary>
        public int Degree{get;set;}

        /// <summary>
        /// Gamma in kernel function (default 1/k)
        /// </summary>
        public double Gamma{get;set;}

        /// <summary>
        /// Zeroeth coefficient in kernel function (default 0)
        /// </summary>
        public double Coefficient0{get;set;}
		
        /// <summary>
        /// Cache memory size in MB (default 100)
        /// </summary>
        public double CacheSize{get;set;}

        /// <summary>
        /// Tolerance of termination criterion (default 0.001)
        /// </summary>
        public double EPS{get;set;}

        /// <summary>
        /// The parameter C of C-SVC, epsilon-SVR, and nu-SVR (default 1)
        /// </summary>
        public double C{get;set;}

        /// <summary>
        /// Contains custom weights for class labels.  Default weight value is 1.
        /// </summary>
        public Dictionary<int,double> Weights{get; private set;}

        /// <summary>
        /// The parameter nu of nu-SVC, one-class SVM, and nu-SVR (default 0.5)
        /// </summary>
        public double Nu{get;set;}

        /// <summary>
        /// The epsilon in loss function of epsilon-SVR (default 0.1)
        /// </summary>
        public double P{get;set;}

        /// <summary>
        /// Whether to use the shrinking heuristics, (default True)
        /// </summary>
        public bool Shrinking{get;set;}

        /// <summary>
        /// Whether to train an SVC or SVR model for probability estimates, (default False)
        /// </summary>
        public bool Probability{get;set;}

        public override bool Equals(object obj)
        {
            Parameter other = obj as Parameter;
            if (other == null)
                return false;

            return other.C == C &&
                other.CacheSize == CacheSize &&
                other.Coefficient0 == Coefficient0 &&
                other.Degree == Degree &&
                other.EPS == EPS &&
                other.Gamma == Gamma &&
                other.KernelType == KernelType &&
                other.Nu == Nu &&
                other.P == P &&
                other.Probability == Probability &&
                other.Shrinking == Shrinking &&
                other.SvmType == SvmType &&
                other.Weights.ToArray().IsEqual(Weights.ToArray());
        }

        public override int GetHashCode()
        {
            return C.GetHashCode() +
                CacheSize.GetHashCode() +
                Coefficient0.GetHashCode() +
                Degree.GetHashCode() +
                EPS.GetHashCode() +
                Gamma.GetHashCode() +
                KernelType.GetHashCode() +
                Nu.GetHashCode() +
                P.GetHashCode() +
                Probability.GetHashCode() +
                Shrinking.GetHashCode() +
                SvmType.GetHashCode() +
                Weights.ToArray().ComputeHashcode();
        }


        #region ICloneable Members
        /// <summary>
        /// Creates a memberwise clone of this parameters object.
        /// </summary>
        /// <returns>The clone (as type Parameter)</returns>
        public object Clone()
        {
            return base.MemberwiseClone();
        }

        #endregion
    }
}