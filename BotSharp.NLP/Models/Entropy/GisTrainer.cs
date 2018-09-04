//Copyright (C) 2005 Richard J. Northedge
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

//This file is based on the GISTrainer.java source file found in the
//original java implementation of MaxEnt.  That source file contains the following header:

// Copyright (C) 2001 Jason Baldridge and Gann Bierner
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

using System;
using System.Collections;
using System.Collections.Generic;

namespace BotSharp.Models
{
	/// <summary>
	/// An implementation of Generalized Iterative Scaling.  The reference paper
	/// for this implementation was Adwait Ratnaparkhi's tech report at the
	/// University of Pennsylvania's Institute for Research in Cognitive Science,
	/// and is available at <a href ="ftp://ftp.cis.upenn.edu/pub/ircs/tr/97-08.ps.Z"><code>ftp://ftp.cis.upenn.edu/pub/ircs/tr/97-08.ps.Z</code></a>. 
	/// </summary>
	/// <author>
	/// Jason Baldridge
	/// </author>
	/// <author>
	///  Richard J, Northedge
	/// </author>
	/// <version>
	/// based on GISTrainer.java, $Revision: 1.15 $, $Date: 2004/06/14 20:52:41 $
	/// </version>
	public class GisTrainer : IO.IGisModelReader
	{
		private int mTokenCount; // # of event tokens
		private int mPredicateCount; // # of predicates
		private int mOutcomeCount; // # of mOutcomes
		private int mTokenID; // global index variable for Tokens
		private int mPredicateId; // global index variable for Predicates    
		private int mOutcomeId; // global index variable for Outcomes
				
		// records the array of predicates seen in each event
		private int[][] mContexts;
		
		// records the array of outcomes seen in each event
		private int[] mOutcomes;
				
		// records the num of times an event has been seen, paired to
		// int[][] mContexts
		private int[] mNumTimesEventsSeen;
		
		// stores the string names of the outcomes.  The GIS only tracks outcomes
		// as ints, and so this array is needed to save the model to disk and
		// thereby allow users to know what the outcome was in human
		// understandable terms.
		private string[] mOutcomeLabels;
		
		// stores the string names of the predicates. The GIS only tracks
		// predicates as ints, and so this array is needed to save the model to
		// disk and thereby allow users to know what the outcome was in human
		// understandable terms.
		private string[] mPredicateLabels;

		// stores the observed expections of each of the events
		private double[][] mObservedExpections;
		
		// stores the estimated parameter value of each predicate during iteration
		private double[][] mParameters;
		
		// Stores the expected values of the features based on the current models
		private double[][] mModelExpections;
		
		//The maximum number of features fired in an event. Usually referred to as C.
		private int mMaximumFeatureCount;

		// stores inverse of constant, 1/C.
		private double mMaximumFeatureCountInverse;

		// the correction parameter of the model
		private double mCorrectionParameter;

		// observed expectation of correction feature
		private double mCorrectionFeatureObservedExpectation;

		// a global variable to help compute the amount to modify the correction
		// parameter
		private double mCorrectionFeatureModifier;
		
		private const double mNearZero = 0.01;
		private const double mLLThreshold = 0.0001;
		
		// Stores the output of the current model on a single event durring
		// training.  This will be reset for every event for every iteration.
		private double[] mModelDistribution;

		// Stores the number of features that get fired per event
		private int[] mFeatureCounts;

		// initial probability for all outcomes.
		private double mInitialProbability;
		
		private Dictionary<string, PatternedPredicate> mPredicates;
		private int[][] mOutcomePatterns;

		// smoothing algorithm (unused) --------

//		internal class UpdateParametersWithSmoothingProcedure : Trove.IIntDoubleProcedure
//		{

//			private double mdSigma = 2.0;

//			public UpdateParametersWithSmoothingProcedure(GisTrainer enclosingInstance)
//			{
//				moEnclosingInstance = enclosingInstance;
//			}
//		
//			private GisTrainer moEnclosingInstance;
//
//			public virtual bool Execute(int outcomeID, double input)
//			{
//				double x = 0.0;
//				double x0 = 0.0;
//				double tmp;
//				double f;
//				double fp;
//				for (int i = 0; i < 50; i++) 
//				{
//					// check what domain these parameters are in
//					tmp = moEnclosingInstance.maoModelExpections[moEnclosingInstance.miPredicateID][outcomeID] * System.Math.Exp(moEnclosingInstance.miConstant * x0);
//					f = tmp + (input + x0) / moEnclosingInstance.mdSigma - moEnclosingInstance.maoObservedExpections[moEnclosingInstance.miPredicateID][outcomeID];
//					fp = tmp * moEnclosingInstance.miConstant + 1 / moEnclosingInstance.mdSigma;
//					if (fp == 0) 
//					{
//						break;
//					}
//					x = x0 - f / fp;
//					if (System.Math.Abs(x - x0) < 0.000001) 
//					{
//						x0 = x;
//						break;
//					}
//					x0 = x;
//				}
//				moEnclosingInstance.maoParameters[moEnclosingInstance.miPredicateID].Put(outcomeID, input + x0);
//				return true;
//			}
//		}


		// training progress event -----------

		/// <summary>
		/// Used to provide informational messages regarding the
		/// progress of the training algorithm.
		/// </summary>
		public event TrainingProgressEventHandler TrainingProgress;

		/// <summary>
		/// Used to raise events providing messages with information
		/// about training progress.
		/// </summary>
		/// <param name="e">
		/// Contains the message with information about the progress of 
		/// the training algorithm.
		/// </param>
		protected virtual void OnTrainingProgress(TrainingProgressEventArgs e) 
		{
			if (TrainingProgress != null) 
			{
				TrainingProgress(this, e); 
			}
		}

		private void NotifyProgress(string message)
		{
			OnTrainingProgress(new TrainingProgressEventArgs(message));
		}


		// training options --------------
        
    	/// <summary>
    	/// Sets whether this trainer will use smoothing while training the model.
		/// This can improve model accuracy, though training will potentially take
		/// longer and use more memory.  Model size will also be larger.
		/// </summary>
		/// <remarks>
		/// Initial testing indicates improvements for models built on small data sets and
		/// few outcomes, but performance degradation for those with large data
		/// sets and lots of outcomes.
		/// </remarks>
		public bool Smoothing { get; set; }

		/// <summary>
		/// Sets whether this trainer will use slack parameters while training the model.
		/// </summary>
		public bool UseSlackParameter { get; set; }

		/// <summary>
		/// If smoothing is in use, this value indicates the "number" of
		/// times we want the trainer to imagine that it saw a feature that it
		/// actually didn't see.  Defaulted to 0.1.
		/// </summary>
		public double SmoothingObservation { get; set; }
		
		/// <summary>
		/// Creates a new <code>GisTrainer</code> instance.
		/// </summary>
		public GisTrainer()
		{
			Smoothing = false;
			UseSlackParameter = false;
			SmoothingObservation = 0.1;
		}

		/// <summary>
		/// Creates a new <code>GisTrainer</code> instance.
		/// </summary>
		/// <param name="useSlackParameter">
		/// Sets whether this trainer will use slack parameters while training the model.
		/// </param>
		public GisTrainer(bool useSlackParameter)
		{
			Smoothing = false;
			UseSlackParameter = useSlackParameter;
			SmoothingObservation = 0.1;
		}

		/// <summary>
		/// Creates a new <code>GisTrainer</code> instance.
		/// </summary>
		/// <param name="smoothingObservation">
		/// If smoothing is in use, this value indicates the "number" of
		/// times we want the trainer to imagine that it saw a feature that it
		/// actually didn't see.  Defaulted to 0.1.
		/// </param>
		public GisTrainer(double smoothingObservation)
		{
			Smoothing = true;
			UseSlackParameter = false;
			SmoothingObservation = smoothingObservation;
		}
		
		/// <summary>
		/// Creates a new <code>GisTrainer</code> instance.
		/// </summary>
		/// <param name="useSlackParameter">
		/// Sets whether this trainer will use slack parameters while training the model.
		/// </param>
		/// <param name="smoothingObservation">
		/// If smoothing is in use, this value indicates the "number" of
		/// times we want the trainer to imagine that it saw a feature that it
		/// actually didn't see.  Defaulted to 0.1.
		/// </param>
		public GisTrainer(bool useSlackParameter, double smoothingObservation)
		{
			Smoothing = true;
			UseSlackParameter = useSlackParameter;
			SmoothingObservation = smoothingObservation;
		}


		// alternative TrainModel signatures --------------

		/// <summary>
		/// Train a model using the GIS algorithm.
		/// </summary>
		/// <param name="eventReader">
		/// The ITrainingEventReader holding the data on which this model
		/// will be trained.
		/// </param>
		public virtual void TrainModel(ITrainingEventReader eventReader)
		{
			TrainModel(eventReader, 100, 0);
		}

		/// <summary>
		/// Train a model using the GIS algorithm.
		/// </summary>
		/// <param name="eventReader">
		/// The ITrainingEventReader holding the data on which this model will be trained
		/// </param>
		/// <param name="iterations">The number of GIS iterations to perform</param>
		/// <param name="cutoff">
		/// The number of times a predicate must be seen in order
		/// to be relevant for training.
		/// </param>
		public virtual void TrainModel(ITrainingEventReader eventReader, int iterations, int cutoff)
		{
			TrainModel(iterations, new OnePassDataIndexer(eventReader, cutoff));
		}
		
		
		// training algorithm -----------------------------

		/// <summary>
		/// Train a model using the GIS algorithm.
		/// </summary>
		/// <param name="iterations">
		/// The number of GIS iterations to perform.
		/// </param>
		/// <param name="dataIndexer">
		/// The data indexer used to compress events in memory.
		/// </param>
		public virtual void TrainModel(int iterations, ITrainingDataIndexer dataIndexer)
		{
			int[] outcomeList;

			//incorporate all of the needed info
			NotifyProgress("Incorporating indexed data for training...");
			mContexts = dataIndexer.GetContexts();
			mOutcomes = dataIndexer.GetOutcomeList();
			mNumTimesEventsSeen = dataIndexer.GetNumTimesEventsSeen();
			mTokenCount = mContexts.Length;
			
			// determine the correction constant and its inverse
			mMaximumFeatureCount = mContexts[0].Length;
			for (mTokenID = 1; mTokenID < mContexts.Length; mTokenID++)
			{
				if (mContexts[mTokenID].Length > mMaximumFeatureCount)
				{
					mMaximumFeatureCount = mContexts[mTokenID].Length;
				}
			}
			mMaximumFeatureCountInverse = 1.0 / mMaximumFeatureCount;
			
			NotifyProgress("done.");
			
			mOutcomeLabels = dataIndexer.GetOutcomeLabels();
			outcomeList = dataIndexer.GetOutcomeList();
			mOutcomeCount = mOutcomeLabels.Length;
			mInitialProbability = Math.Log(1.0 / mOutcomeCount);
			
			mPredicateLabels = dataIndexer.GetPredicateLabels();
			mPredicateCount = mPredicateLabels.Length;
			
			NotifyProgress("\tNumber of Event Tokens: " + mTokenCount);
			NotifyProgress("\t    Number of Outcomes: " + mOutcomeCount);
			NotifyProgress("\t  Number of Predicates: " + mPredicateCount);
			
			// set up feature arrays
			var predicateCounts = new int[mPredicateCount][];
			for (mPredicateId = 0; mPredicateId < mPredicateCount; mPredicateId++)
			{
				predicateCounts[mPredicateId] = new int[mOutcomeCount];
			}
			for (mTokenID = 0; mTokenID < mTokenCount; mTokenID++)
			{
				for (int currentContext = 0; currentContext < mContexts[mTokenID].Length; currentContext++)
				{
					predicateCounts[mContexts[mTokenID][currentContext]][outcomeList[mTokenID]] += mNumTimesEventsSeen[mTokenID];
				}
			}

		    // A fake "observation" to cover features which are not detected in
			// the data.  The default is to assume that we observed "1/10th" of a
			// feature during training.
			double smoothingObservation = SmoothingObservation;
			
			// Get the observed expectations of the features. Strictly speaking,
			// we should divide the counts by the number of Tokens, but because of
			// the way the model's expectations are approximated in the
			// implementation, this is cancelled out when we compute the next
			// iteration of a parameter, making the extra divisions wasteful.
			mOutcomePatterns = new int[mPredicateCount][];
			mParameters = new double[mPredicateCount][];
			mModelExpections = new double[mPredicateCount][];
			mObservedExpections = new double[mPredicateCount][];

		    for (mPredicateId = 0; mPredicateId < mPredicateCount; mPredicateId++)
			{
			    int activeOutcomeCount;
			    if (Smoothing)
				{
					activeOutcomeCount = mOutcomeCount;
				}
				else
				{
					activeOutcomeCount = 0;
					for (mOutcomeId = 0; mOutcomeId < mOutcomeCount; mOutcomeId++)
					{
						if (predicateCounts[mPredicateId][mOutcomeId] > 0)
						{
							activeOutcomeCount++;
						}
					}
				}

				mOutcomePatterns[mPredicateId] = new int[activeOutcomeCount];
				mParameters[mPredicateId] = new double[activeOutcomeCount];
				mModelExpections[mPredicateId] = new double[activeOutcomeCount];
				mObservedExpections[mPredicateId] = new double[activeOutcomeCount];

				int currentOutcome = 0;
				for (mOutcomeId = 0; mOutcomeId < mOutcomeCount; mOutcomeId++)
				{
					if (predicateCounts[mPredicateId][mOutcomeId] > 0)
					{
						mOutcomePatterns[mPredicateId][currentOutcome] = mOutcomeId;
						mObservedExpections[mPredicateId][currentOutcome] = Math.Log(predicateCounts[mPredicateId][mOutcomeId]);
						currentOutcome++;
					}
					else if (Smoothing)
					{
						mOutcomePatterns[mPredicateId][currentOutcome] = mOutcomeId;
						mObservedExpections[mPredicateId][currentOutcome] = Math.Log(smoothingObservation);
						currentOutcome++;
					}
				}
			}
			
			// compute the expected value of correction
			if (UseSlackParameter) 
			{
				int correctionFeatureValueSum = 0;
				for (mTokenID = 0; mTokenID < mTokenCount; mTokenID++)
				{
					for (int currentContext = 0; currentContext < mContexts[mTokenID].Length; currentContext++)
					{
						mPredicateId = mContexts[mTokenID][currentContext];

						if ((!Smoothing) && predicateCounts[mPredicateId][mOutcomes[mTokenID]] == 0)
						{
							correctionFeatureValueSum += mNumTimesEventsSeen[mTokenID];
						}
					}
					correctionFeatureValueSum += (mMaximumFeatureCount - mContexts[mTokenID].Length) * mNumTimesEventsSeen[mTokenID];
				}
				if (correctionFeatureValueSum == 0)
				{
					mCorrectionFeatureObservedExpectation = Math.Log(mNearZero); //nearly zero so log is defined
				}
				else
				{
					mCorrectionFeatureObservedExpectation = Math.Log(correctionFeatureValueSum);
				}
			
				mCorrectionParameter = 0.0;
			}

		    NotifyProgress("...done.");
			
			mModelDistribution = new double[mOutcomeCount];
			mFeatureCounts = new int[mOutcomeCount];
			
			//Find the parameters
			NotifyProgress("Computing model parameters...");
			FindParameters(iterations);
			
			NotifyProgress("Converting to new predicate format...");
			ConvertPredicates();

		}
		
		/// <summary>
		/// Estimate and return the model parameters.
		/// </summary>
		/// <param name="iterations">
		/// Number of iterations to run through.
		/// </param>
		private void FindParameters(int iterations)
		{
			double previousLogLikelihood = 0.0;
		    NotifyProgress("Performing " + iterations + " iterations.");
			for (int currentIteration = 1; currentIteration <= iterations; currentIteration++)
			{
				if (currentIteration < 10)
				{
					NotifyProgress("  " + currentIteration + ":  ");
				}
				else if (currentIteration < 100)
				{
					NotifyProgress(" " + currentIteration + ":  ");
				}
				else
				{
					NotifyProgress(currentIteration + ":  ");
				}
				double currentLogLikelihood = NextIteration();
				if (currentIteration > 1)
				{
					if (previousLogLikelihood > currentLogLikelihood)
					{
						throw new SystemException("Model Diverging: loglikelihood decreased");
					}
					if (currentLogLikelihood - previousLogLikelihood < mLLThreshold)
					{
						break;
					}
				}
				previousLogLikelihood = currentLogLikelihood;
			}
			
			// kill a bunch of these big objects now that we don't need them
			mObservedExpections = null;
			mModelExpections = null;
			mNumTimesEventsSeen = null;
			mContexts = null;
		}
		
		/// <summary>
		/// Use this model to evaluate a context and return an array of the
		/// likelihood of each outcome given that context.
		/// </summary>
		/// <param name="context">
		/// The integers of the predicates which have been
		/// observed at the present decision point.
		/// </param>
		/// <param name="outcomeSums">
		/// The normalized probabilities for the outcomes given the
		/// context. The indexes of the double[] are the outcome
		/// ids.
		/// </param>
		protected virtual void Evaluate(int[] context, double[] outcomeSums)
		{
			for (int outcomeIndex = 0; outcomeIndex < mOutcomeCount; outcomeIndex++)
			{
				outcomeSums[outcomeIndex] = mInitialProbability;
				mFeatureCounts[outcomeIndex] = 0;
			}
			int[] activeOutcomes;
			int outcomeId;
			int predicateId;
			int currentActiveOutcome;

			for (int currentContext = 0; currentContext < context.Length; currentContext++)
			{
				predicateId = context[currentContext];
				activeOutcomes = mOutcomePatterns[predicateId];
				for (currentActiveOutcome = 0; currentActiveOutcome < activeOutcomes.Length; currentActiveOutcome++)
				{
					outcomeId = activeOutcomes[currentActiveOutcome];
					mFeatureCounts[outcomeId]++;
					outcomeSums[outcomeId] += mMaximumFeatureCountInverse * mParameters[predicateId][currentActiveOutcome];
				}
			}
			
			double sum = 0.0;
			for (int currentOutcomeId = 0; currentOutcomeId < mOutcomeCount; currentOutcomeId++)
			{
				outcomeSums[currentOutcomeId] = System.Math.Exp(outcomeSums[currentOutcomeId]);
				if (UseSlackParameter) 
				{
					outcomeSums[currentOutcomeId] += ((1.0 - ((double) mFeatureCounts[currentOutcomeId] / mMaximumFeatureCount)) * mCorrectionParameter);
				}
				sum += outcomeSums[currentOutcomeId];
			}
			
			for (int currentOutcomeId = 0; currentOutcomeId < mOutcomeCount; currentOutcomeId++)
			{
				outcomeSums[currentOutcomeId] /= sum;
			}
		}
				
		/// <summary>
		/// Compute one iteration of GIS and retutn log-likelihood.
		/// </summary>
		/// <returns>The log-likelihood.</returns>
		private double NextIteration()
		{
			// compute contribution of p(a|b_i) for each feature and the new
			// correction parameter
			double logLikelihood = 0.0;
			mCorrectionFeatureModifier = 0.0;
			int eventCount = 0;
			int numCorrect = 0;
			int outcomeId;

            for (mTokenID = 0; mTokenID < mTokenCount; mTokenID++)
            {
                Evaluate(mContexts[mTokenID], mModelDistribution);
                for (int currentContext = 0; currentContext < mContexts[mTokenID].Length; currentContext++)
                {
                    mPredicateId = mContexts[mTokenID][currentContext];
                    for (int currentActiveOutcome = 0; currentActiveOutcome < mOutcomePatterns[mPredicateId].Length; currentActiveOutcome++)
                    {
                        outcomeId = mOutcomePatterns[mPredicateId][currentActiveOutcome];
                        mModelExpections[mPredicateId][currentActiveOutcome] += (mModelDistribution[outcomeId] * mNumTimesEventsSeen[mTokenID]);

                        if (UseSlackParameter)
                        {
                            mCorrectionFeatureModifier += mModelDistribution[mOutcomeId] * mNumTimesEventsSeen[mTokenID];
                        }
                    }
                }

				if (UseSlackParameter)
				{
					mCorrectionFeatureModifier += (mMaximumFeatureCount - mContexts[mTokenID].Length) * mNumTimesEventsSeen[mTokenID];
				}

				logLikelihood += System.Math.Log(mModelDistribution[mOutcomes[mTokenID]]) * mNumTimesEventsSeen[mTokenID];
				eventCount += mNumTimesEventsSeen[mTokenID];
				
				//calculation solely for the information messages
				int max = 0;
				for (mOutcomeId = 1; mOutcomeId < mOutcomeCount; mOutcomeId++)
				{
					if (mModelDistribution[mOutcomeId] > mModelDistribution[max])
					{
						max = mOutcomeId;
					}
				}
				if (max == mOutcomes[mTokenID])
				{
					numCorrect += mNumTimesEventsSeen[mTokenID];
				}
			}
			NotifyProgress(".");
			
			// compute the new parameter values
			for (mPredicateId = 0; mPredicateId < mPredicateCount; mPredicateId++)
			{
				for (int currentActiveOutcome = 0; currentActiveOutcome < mOutcomePatterns[mPredicateId].Length; currentActiveOutcome++)
				{
					outcomeId = mOutcomePatterns[mPredicateId][currentActiveOutcome];
					mParameters[mPredicateId][currentActiveOutcome] += (mObservedExpections[mPredicateId][currentActiveOutcome] - Math.Log(mModelExpections[mPredicateId][currentActiveOutcome]));
					mModelExpections[mPredicateId][currentActiveOutcome] = 0.0;// re-initialize to 0.0's
				}
			}

			if (mCorrectionFeatureModifier > 0.0 && UseSlackParameter)
			{
				mCorrectionParameter += (mCorrectionFeatureObservedExpectation - Math.Log(mCorrectionFeatureModifier));
			}

			NotifyProgress(". logLikelihood=" + logLikelihood + "\t" + ((double) numCorrect / eventCount));
			return (logLikelihood);
		}
		
		/// <summary>
		/// Convert the predicate data into the outcome pattern / patterned predicate format used by the GIS models.
		/// </summary>
		private void ConvertPredicates()
		{
			var predicates = new PatternedPredicate[mParameters.Length];
			
			for (mPredicateId = 0; mPredicateId < mPredicateCount; mPredicateId++)
			{
				double[] parameters = mParameters[mPredicateId];
				predicates[mPredicateId] = new PatternedPredicate(mPredicateLabels[mPredicateId], parameters);
			}

			var comparer = new OutcomePatternComparer();
			Array.Sort(mOutcomePatterns, predicates, comparer);

            List<int[]> outcomePatterns = new List<int[]>();
			int currentPatternId = 0;
			int predicatesInPattern = 0;
			int[] currentPattern = mOutcomePatterns[0];

			for (mPredicateId = 0; mPredicateId < mPredicateCount; mPredicateId++)
			{
				if (comparer.Compare(currentPattern, mOutcomePatterns[mPredicateId]) == 0)
				{
					predicates[mPredicateId].OutcomePattern = currentPatternId;
					predicatesInPattern++;
				}
				else
				{
					int[] pattern = new int[currentPattern.Length + 1];
					pattern[0] = predicatesInPattern;
					currentPattern.CopyTo(pattern, 1);
					outcomePatterns.Add(pattern);
					currentPattern = mOutcomePatterns[mPredicateId];
					currentPatternId++;
					predicates[mPredicateId].OutcomePattern = currentPatternId;
					predicatesInPattern = 1;
				}
			}
			int[] finalPattern = new int[currentPattern.Length + 1];
			finalPattern[0] = predicatesInPattern;
			currentPattern.CopyTo(finalPattern, 1);
			outcomePatterns.Add(finalPattern);

			mOutcomePatterns = outcomePatterns.ToArray();
            mPredicates = new Dictionary<string, PatternedPredicate>(predicates.Length);
			for (mPredicateId = 0; mPredicateId < mPredicateCount; mPredicateId++)
			{
				mPredicates.Add(predicates[mPredicateId].Name, predicates[mPredicateId]);
			}
		}

        
		// IGisModelReader implementation --------------------
		
		/// <summary>
		/// The correction constant for the model produced as a result of training.
		/// </summary>
		public int CorrectionConstant
		{
			get
			{
				return mMaximumFeatureCount;
			}
		}
	
		/// <summary>
		/// The correction parameter for the model produced as a result of training.
		/// </summary>
		public double CorrectionParameter
		{
			get
			{
				return mCorrectionParameter;
			}
		}
	
		/// <summary>
		/// Obtains the outcome labels for the model produced as a result of training.
		/// </summary>
		/// <returns>
		/// Array of outcome labels.
		/// </returns>
		public string[] GetOutcomeLabels()
		{
			return mOutcomeLabels;
		}
	
		/// <summary>
		/// Obtains the outcome patterns for the model produced as a result of training.
		/// </summary>
		/// <returns>
		/// Array of outcome patterns.
		/// </returns>
		public int[][] GetOutcomePatterns()
		{
			return mOutcomePatterns;
		}

		/// <summary>
		/// Obtains the predicate data for the model produced as a result of training.
		/// </summary>
		/// <returns>
		/// Dictionary containing PatternedPredicate objects.
		/// </returns>
        public Dictionary<string, PatternedPredicate> GetPredicates()
		{
			return mPredicates;
		}

		/// <summary>
		/// Returns trained model information for a predicate, given the predicate label.
		/// </summary>
		/// <param name="predicateLabel">
		/// The predicate label to fetch information for.
		/// </param>
		/// <param name="featureCounts">
		/// Array to be passed in to the method; it should have a length equal to the number of outcomes
		/// in the model.  The method increments the count of each outcome that is active in the specified
		/// predicate.
		/// </param>
		/// <param name="outcomeSums">
		/// Array to be passed in to the method; it should have a length equal to the number of outcomes
		/// in the model.  The method adds the parameter values for each of the active outcomes in the
		/// predicate.
		/// </param>
		public void GetPredicateData(string predicateLabel, int[] featureCounts, double[] outcomeSums)
		{
            if (mPredicates.ContainsKey(predicateLabel))
            {
                PatternedPredicate predicate = mPredicates[predicateLabel];
                if (predicate != null)
                {
                    int[] activeOutcomes = mOutcomePatterns[predicate.OutcomePattern];

                    for (int currentActiveOutcome = 1; currentActiveOutcome < activeOutcomes.Length; currentActiveOutcome++)
                    {
                        int outcomeIndex = activeOutcomes[currentActiveOutcome];
                        featureCounts[outcomeIndex]++;
                        outcomeSums[outcomeIndex] += predicate.GetParameter(currentActiveOutcome - 1);
                    }
                } 
            }
		}
		

		private class OutcomePatternComparer : IComparer<int[]>
		{

			internal OutcomePatternComparer()
			{
			}

			/// <summary>
			/// Compare two outcome patterns and determines which comes first,
			/// based on the outcome ids (lower outcome ids first)
			/// </summary>
            /// <param name="firstPattern">
			/// First outcome pattern to compare.
			/// </param>
            /// <param name="secondPattern">
			/// Second outcome pattern to compare.
			/// </param>
			/// <returns></returns>
            public virtual int Compare(int[] firstPattern, int[] secondPattern)
			{			
				int smallerLength = (firstPattern.Length > secondPattern.Length ? secondPattern.Length : firstPattern.Length);
			
				for (int currentOutcome = 0; currentOutcome < smallerLength; currentOutcome++)
				{
					if (firstPattern[currentOutcome] < secondPattern[currentOutcome])
					{
						return - 1;
					}
					else if (firstPattern[currentOutcome] > secondPattern[currentOutcome])
					{
						return 1;
					}
				}
			
				if (firstPattern.Length < secondPattern.Length)
				{
					return - 1;
				}
				else if (firstPattern.Length > secondPattern.Length)
				{
					return 1;
				}
			
				return 0;
			}
		}
	}

	/// <summary>
	/// Event arguments class for training progress events.
	/// </summary>
	public class TrainingProgressEventArgs : EventArgs
	{
		private string mMessage;
	
		/// <summary>
		/// Constructor for the training progress event arguments.
		/// </summary>
		/// <param name="message">
		/// Information message about the progress of training.
		/// </param>
		public TrainingProgressEventArgs(string message)
		{
			mMessage = message;
		}

		/// <summary>
		/// Information message about the progress of training.
		/// </summary>
		public string Message 
		{
			get
			{
				return mMessage;
			}
		}
	}

	/// <summary>
	/// Event handler delegate for the training progress event.
	/// </summary>
	public delegate void TrainingProgressEventHandler(object sender, TrainingProgressEventArgs e);


}
