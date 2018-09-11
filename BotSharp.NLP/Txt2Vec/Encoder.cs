using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
//using AdvUtils;

namespace Txt2Vec
{
    public enum WORD_SOURCE
    {
        CORPUS,
        PRETRAINED_MODEL
    }

    public class vocab_word
    {
        public string word;
        public int cnt;
        public WORD_SOURCE source;
    }


    public class Encoder
    {
        const int EXP_TABLE_SIZE = 1000;
        const int MAX_EXP = 6;
        const int MAX_CODE_LENGTH = 40;

        StreamReader srTrainCorpus = null;
        Dictionary<string, int> word2id;
        List<vocab_word> vocab;
        int vocab_size = 0;

        long train_words = 0;
        long word_count_actual = 0;
        long next_save_step = 10000000;
        long next_save_trained_words = 10000000;
        long sentence_count = 0;

        public long iter = 5;
        public int layer1_size = 200;
        public double starting_alpha = 0.025;
        public double sample = 0;
        public int min_count = 5;
        public int num_threads = 1;
        public int cbow = 1, window = 5;
        public int classes = 1;
        public int debug_mode = 0;
        public long savestep = 100000000;
        public int negative = 5;
        public string strPreTrainedModelFileName = null;
        public int onlyUpdateCorpusWord = 0;

        double[] syn0;
        double[] syn1;
        double[] totalNeu_e;
        double[] expTable;
        object[] syn0Locker;
        object[] syn1Locker;

        Random rand = new Random(DateTime.Today.Millisecond);

        public int[] accFreqTable;
        public int accTotalFreq = 0;
        public int accFactor = 1;

        void InitAccTermFreq()
        {
            //Logger.WriteLine("Initializing acculumate term frequency...");
            accFreqTable = new int[vocab_size];
            accTotalFreq = 0;

            //Keep accTotalFreq is less than int.MaxValue
            accFactor = 1 + (int)(train_words / int.MaxValue);
            //Logger.WriteLine("Acculumate factor: {0}", accFactor);

            int i = 0;
            foreach (vocab_word word in vocab)
            {
                accTotalFreq += (word.cnt / accFactor);
                accFreqTable[i] = accTotalFreq;
                i++;
            }

            //Logger.WriteLine("Acculumated total frequency : {0}", accTotalFreq);
        }

        int SearchAccTermTable(int freq)
        {
            int mid = vocab_size >> 1;
            int left = 0, right = vocab_size - 1;

            while (true)
            {
                if (accFreqTable[mid] < freq)
                {
                    left = mid + 1;
                }
                else if (accFreqTable[mid] > freq)
                {
                    if (mid == 0)
                    {
                        return 0;
                    }

                    if (accFreqTable[mid - 1] < freq)
                    {
                        return mid;
                    }

                    right = mid - 1;
                }
                else
                {
                    return mid;
                }

                mid = (left + right) >> 1;
            }
        }


        public Encoder()
        {
            word2id = new Dictionary<string, int>();
            vocab = new List<vocab_word>();

            expTable = new double[EXP_TABLE_SIZE + 1];

            for (int i = 0; i < EXP_TABLE_SIZE; i++)
            {
                expTable[i] = Math.Exp((i / (double)EXP_TABLE_SIZE * 2 - 1) * MAX_EXP); // Precompute the exp() table
                expTable[i] = expTable[i] / (expTable[i] + 1);                   // Precompute f(x) = x / (x + 1)
            }

        }

        // Returns position of a word in the vocabulary; if the word is not found, returns -1
        int SearchVocab(string word)
        {
            if (word2id.ContainsKey(word) == false)
            {
                return -1;
            }
            return word2id[word];
        }

        public class VocabComparer : IComparer<vocab_word>
        {
            public int Compare(vocab_word x, vocab_word y)
            {
                return y.cnt.CompareTo(x.cnt);
            }
        }

        // Shrink the vocabulary by frequency using word counts
        void ShrinkVocab()
        {
            // Sort the vocabulary
            vocab.Sort(new VocabComparer());

            word2id.Clear();
            int size = vocab_size;
            train_words = 0;
            for (int a = 0; a < size; a++)
            {
                // Words occuring less than min_count times will be discarded from the vocab
                if (vocab[a].cnt < min_count)
                {
                    vocab_size--;
                    vocab[a].word = null;
                }
                else
                {
                    word2id.Add(vocab[a].word, a);
                    train_words += vocab[a].cnt;
                }
            }

            vocab.RemoveRange(vocab_size, vocab.Count - vocab_size);
        }

        void LoadPreTrainModelSyn(string strModelFileName, double[] syn)
        {
            Model preTrainedModel = new Model();
            preTrainedModel.LoadModel(strModelFileName, false);


            if (preTrainedModel.VectorSize != layer1_size)
            {
                throw new Exception("The layer size is inconsistent between given parameter and pre-trained model.");
            }

            string[] allTerms = preTrainedModel.GetAllTerms();
            foreach (string strTerm in allTerms)
            {
                int wordId = SearchVocab(strTerm);
                if (wordId < 0)
                {
                    //Ingore the dropped term
                    continue;
                }
                
                float[] vector = preTrainedModel.GetVector(strTerm);
                for (int i = 0; i < layer1_size;i++)
                {
                    syn[i + wordId * layer1_size] = vector[i];
                }
            }

        }

        void LoadVocabFromPreTrainModel(string strModelFileName)
        {
            Model preTrainedModel = new Model();
            preTrainedModel.LoadModel(strModelFileName, false);

            layer1_size = preTrainedModel.VectorSize;
            //Logger.WriteLine("Apply the following options from pr-trained model file {0}", preTrainedModel);
            //Logger.WriteLine("Vector Size: {0}", layer1_size);

            string[] allTerms = preTrainedModel.GetAllTerms();
            foreach (string strTerm in allTerms)
            {
                //Add terms in pre-trained model into vocabulary
                //If the term is already added from corpus or given dictionary, we ignore it
                if (word2id.ContainsKey(strTerm) == false)
                {
                    Term term = preTrainedModel.GetTerm(strTerm);
                    vocab_word word = new vocab_word();
                    word.word = strTerm;
                    word.cnt = 0;
                    word.source = WORD_SOURCE.PRETRAINED_MODEL;

                    word2id.Add(word.word, vocab_size);
                    vocab.Add(word);

                    vocab_size++;
                }
            }
        }

        public void LoadVocabFromFile(string vocab_file)
        {
            StreamReader sr = new StreamReader(vocab_file);
            string strLine = null;

            word2id = new Dictionary<string, int>();
            vocab = new List<vocab_word>();
            vocab_size = 0;

            while ((strLine = sr.ReadLine()) != null)
            {
                string[] items = strLine.Split('\t');
                vocab_word word = new vocab_word();
                word.word = items[0];                
                word.source = WORD_SOURCE.CORPUS;

                word2id.Add(word.word, vocab_size);
                vocab.Add(word);

                vocab_size++;

            }

            sr.Close();
        }


        private void GetTrainWordSize(string train_file)
        {
            StreamReader fin = new StreamReader(train_file);
            string strLine = null;
            train_words = 0;

            foreach (vocab_word vw in vocab)
            {
                vw.cnt = 0;
            }

            while ((strLine = fin.ReadLine()) != null)
            {
                //Append the end of sentence
                strLine = strLine.Trim();
                string[] items = strLine.Split();
                foreach (string item in items)
                {
                    int wordId = SearchVocab(item);
                    if (wordId >= 0)
                    {
                        vocab[wordId].cnt++;

                        if (vocab[wordId].source == WORD_SOURCE.PRETRAINED_MODEL && onlyUpdateCorpusWord == 1)
                        {
                            continue;
                        }

                        train_words++;
                        if (debug_mode > 0 && train_words % 1000000 == 0)
                        {
                            //Logger.WriteLine("{0}M... ", train_words / 1000000);
                        }
                    }
                }
            }

            fin.Close();
        }

        public void LearnVocabFromTrainFile(string train_file)
        {
            StreamReader fin = new StreamReader(train_file);
            string strLine = null;

            vocab_size = 0;
            int i = 0;
            while ((strLine = fin.ReadLine()) != null)
            {
                //Append the end of sentence
                strLine = strLine.Trim();
                string[] items = strLine.Split();

                foreach (string word in items)
                {
                    //This term is normal word
                    train_words++;
                    if (debug_mode > 0 && train_words % 1000000 == 0)
                    {
                        //Logger.WriteLine("{0}M... ", train_words / 1000000);
                    }

                    i = SearchVocab(word);
                    if (i == -1)
                    {
                        word2id.Add(word, vocab_size);

                        vocab_word voc_word = new vocab_word();
                        voc_word.word = word;
                        voc_word.cnt = 1;
                        voc_word.source = WORD_SOURCE.CORPUS;

                        vocab.Add(voc_word);
                        vocab_size++;

                    }
                    else
                    {
                        vocab[i].cnt++;
                    }
                }
            }

            fin.Close();
        }

        public void SaveVocab(string save_vocab_file)
        {
            StreamWriter fo = new StreamWriter(save_vocab_file);
            for (int i = 0; i < vocab_size; i++)
            {
                fo.WriteLine("{0}\t{1}", vocab[i].word, vocab[i].cnt);
            }
            fo.Close();
        }

        void InitNet()
        {
            syn0Locker = new object[vocab_size];
            syn1Locker = new object[vocab_size];
            for (int i = 0; i < vocab_size; i++)
            {
                syn0Locker[i] = new object();
                syn1Locker[i] = new object();
            }

            totalNeu_e = new double[layer1_size];

            syn0 = new double[vocab_size * layer1_size];
            for (long b = 0; b < layer1_size; b++)
            {
                for (long a = 0; a < vocab_size; a++)
                {
                    syn0[a * layer1_size + b] = (rand.NextDouble() - 0.5) / layer1_size;
                }
            }
            syn1 = new double[vocab_size * layer1_size];


        }

        object rdlocker = new object();
        object wrlocker = new object();
        object locker_rand = new object();
        int RandNext(int max)
        {
            lock (locker_rand)
            {
                return rand.Next(max);
            }
        }

        double RandNextDouble()
        {
            lock (locker_rand)
            {
                return rand.NextDouble();
            }
        }

        void TrainModelThread()
        {
            int word_count = 0, last_word_count = 0;
            double alpha = starting_alpha * (1 - word_count_actual / (double)(iter * train_words + 1));

            while (true)
            {
                if (word_count - last_word_count > 10000)
                {
                    last_word_count = word_count;
                    if (debug_mode > 0)
                    {

                        double sumErr = 0;
                        for (int i = 0; i < layer1_size; i++)
                        {
                            sumErr += (totalNeu_e[i] / word_count_actual);
                        }

                        //Logger.WriteLine("Alpha: {0:0.0000}  Prog: {1:0.00}% Words: {2}K Sent: {3}K Error: {4}", alpha,
                         //word_count_actual / (double)(iter * train_words + 1) * 100, word_count_actual / 1024, sentence_count / 1024, sumErr);
                    }

                    if (word_count_actual > next_save_trained_words)
                    {
                        long old_next_save_trained_words = next_save_trained_words;
                        lock (rdlocker)
                        {
                            if (old_next_save_trained_words == next_save_trained_words)
                            {
                                //Logger.WriteLine("Saving temporary word vector into file...");

                                Model.SaveModel("vector_tmp.bin", vocab_size, layer1_size, vocab, syn0);
                                Model.SaveModel("vector_tmp_bin.syn1", vocab_size, layer1_size, vocab, syn1);

                                next_save_trained_words += next_save_step;
                            }
                        }
                    }

                }

                alpha = starting_alpha * (1 - word_count_actual / (double)(iter * train_words + 1));
                if (alpha < starting_alpha * 0.0001)
                {
                    alpha = starting_alpha * 0.0001;
                }

                //Read a line from training corpus
                string strLine = "";
                lock (rdlocker)
                {
                    strLine = srTrainCorpus.ReadLine();
                }
                if (strLine == null)
                {
                    break;
                }

                Interlocked.Increment(ref sentence_count);
                //Parse each word in current sentence
                string[] strWords = strLine.Split();

                bool bIgnore = true;
                for (int i = 0; i < strWords.Length; i++)
                {
                    int wordId = SearchVocab(strWords[i]);
                    if (wordId < 0)
                    {
                        continue;
                    }

                    if (vocab[wordId].source == WORD_SOURCE.CORPUS)
                    {
                        bIgnore = false;
                        break;
                    }

                    if (vocab[wordId].source == WORD_SOURCE.PRETRAINED_MODEL && onlyUpdateCorpusWord == 0)
                    {
                        bIgnore = false;
                        break;
                    }
                }

                if (bIgnore == true)
                {
                    continue;
                }

                for (int sentence_position = 0; sentence_position < strWords.Length; sentence_position++)
                {
                    string strPredictedWord = strWords[sentence_position];
                    int word = SearchVocab(strPredictedWord);
                    if (word < 0)
                    {
                        //Ingore the dropped term
                        continue;
                    }
                    if (vocab[word].source == WORD_SOURCE.CORPUS ||
                        (vocab[word].source == WORD_SOURCE.PRETRAINED_MODEL && onlyUpdateCorpusWord == 0))
                    {
                        word_count++;
                        Interlocked.Increment(ref word_count_actual);
                    }

                    int rnd_window = RandNext(window);
                    if (cbow != 0)
                    {
                        TrainByCBOW(sentence_position, rnd_window, strWords, alpha, word);
                    }
                    else
                    {
                        TrainBySkipGram(sentence_position, rnd_window, strWords, alpha, word);
                    }
                }
            }
        }

        private void TrainBySkipGram(int sentence_position, int b, string[] sen, double alpha, int word)
        {
            double[] neu1e = new double[layer1_size];

            //train skip-gram
            for (int a = b; a < window * 2 + 1 - b; a++)
            {
                int c = sentence_position - window + a;
                if (c < 0 || c >= sen.Length || c == sentence_position)
                {
                    //Invalidated position. out of sentence boundary
                    continue;
                }

                string strNGram = sen[c];
                int wordId = SearchVocab(strNGram);
                if (wordId == -1)
                {
                    continue;
                }


                int l1 = wordId * layer1_size;
                for (c = 0; c < layer1_size; c++)
                {
                    neu1e[c] = 0;
                }

                lock (syn0Locker[wordId])
                {
                    //Negative sampling
                    int target = 0;
                    int label = 1;
                    for (int d = 0; d < negative + 1; d++)
                    {
                        if (d == 0)
                        {
                            target = word;
                            label = 1;
                        }
                        else
                        {
                            target = SearchAccTermTable(RandNext(accTotalFreq));
                            if (target == word)
                            {
                                continue;
                            }
                            label = 0;
                        }


                        long l2 = target * layer1_size;
                        double f = 0;
                        double g;

                        lock (syn1Locker[target])
                        {
                            for (c = 0; c < layer1_size; c++)
                            {
                                f += syn0[c + l1] * syn1[c + l2];
                            }
                            if (f > MAX_EXP)
                            {
                                g = (label - 1) * alpha;
                            }
                            else if (f < -MAX_EXP)
                            {
                                g = (label - 0) * alpha;
                            }
                            else g = (label - expTable[(int)((f + MAX_EXP) * (EXP_TABLE_SIZE / MAX_EXP / 2))]) * alpha;

                            for (c = 0; c < layer1_size; c++)
                            {
                                neu1e[c] += g * syn1[c + l2];
                            }

                            if (onlyUpdateCorpusWord == 1 && vocab[target].source == WORD_SOURCE.PRETRAINED_MODEL)
                            {
                                continue;
                            }

                            for (c = 0; c < layer1_size; c++)
                            {
                                syn1[c + l2] += g * syn0[c + l1];
                            }
                        }
                    }


                    if (onlyUpdateCorpusWord == 1 && vocab[wordId].source == WORD_SOURCE.PRETRAINED_MODEL)
                    {
                        continue;
                    }

                    // Learn weights input -> hidden
                    for (c = 0; c < layer1_size; c++)
                    {
                        syn0[c + l1] += neu1e[c];
                    }
                }


                for (int i = 0; i < neu1e.Length; i++)
                {
                    totalNeu_e[i] += Math.Abs(neu1e[i] / (window * 2 + 1 - b * 2));
                }

            }
        }

        private void TrainByCBOW(int sentence_position, int b, string[] sen, double alpha, int word)
        {
            double[] neu1 = new double[layer1_size];
            double[] neu1e = new double[layer1_size];
            int cw = 0;
            List<int> wordIdList = new List<int>();

            //train the cbow architecture
            // in -> hidden
            for (int a = b; a < window * 2 + 1 - b; a++)
            {

                int c = sentence_position - window + a;
                if (c < 0 || c >= sen.Length || c == sentence_position)
                {
                    //Invalidated position. out of sentence boundary
                    continue;
                }

                //Generate ngram string and word id
                string strNGram = null;
                int wordId = -1;

                strNGram = sen[c];

                wordId = SearchVocab(strNGram);
                if (wordId < 0)
                {
                    //Ingore the dropped term
                    continue;
                }

                //The subsampling randomly discards frequent words while keeping the ranking same
                if (sample > 0)
                {
                    double ran = (Math.Sqrt(vocab[wordId].cnt / (sample * train_words)) + 1) * (sample * train_words) / vocab[wordId].cnt;
                    if (ran < RandNextDouble())
                    {
                        continue;
                    }
                }

                if (onlyUpdateCorpusWord == 0 || (onlyUpdateCorpusWord == 1 && vocab[wordId].source == WORD_SOURCE.CORPUS))
                {
                    //Terms that need to update their syn0
                    wordIdList.Add(wordId);
                }

                lock (syn0Locker[wordId])
                {
                    for (int t = 0; t < layer1_size; t++)
                    {
                        neu1[t] += syn0[t + wordId * layer1_size];
                    }
                }
                cw++;
            }

            if (wordIdList.Count == 0)
            {
                //No term need to update its syn, return
                return;
            }

            double synUpdateFactor = (double)(cw) / (double)(wordIdList.Count);
            for (int c = 0; c < layer1_size; c++)
            {
                neu1[c] /= cw;
            }


            int target = 0;
            int label = 1;
            for (int d = 0; d < negative + 1; d++)
            {
                if (d == 0)
                {
                    target = word;
                    label = 1;
                }
                else
                {
                    target = SearchAccTermTable(RandNext(accTotalFreq));
                    if (target == word)
                    {
                        continue;
                    }
                    label = 0;
                }

                long l2 = target * layer1_size;
                double f = 0;

                lock (syn1Locker[target])
                {
                    for (int c = 0; c < layer1_size; c++)
                    {
                        f += neu1[c] * syn1[c + l2];
                    }
                    double g = 0;
                    if (f > MAX_EXP) g = (label - 1) * alpha;
                    else if (f < -MAX_EXP) g = (label - 0) * alpha;
                    else g = (label - expTable[(int)((f + MAX_EXP) * (EXP_TABLE_SIZE / MAX_EXP / 2))]) * alpha;

                    for (int c = 0; c < layer1_size; c++)
                    {
                        neu1e[c] += g * syn1[c + l2];
                    }


                    if (onlyUpdateCorpusWord == 1 && vocab[target].source == WORD_SOURCE.PRETRAINED_MODEL)
                    {
                        continue;
                    }

                    for (int c = 0; c < layer1_size; c++)
                    {
                        syn1[c + l2] += g * neu1[c];
                    }
                }
            }


            // hidden -> in
            foreach (int wordId in wordIdList)
            {
                lock (syn0Locker[wordId])
                {
                    for (int c = 0; c < layer1_size; c++)
                    {
                        syn0[c + wordId * layer1_size] += (neu1e[c] * synUpdateFactor);
                    }
                }
            }

            for (int i = 0; i < neu1e.Length; i++)
            {
                totalNeu_e[i] += Math.Abs(neu1e[i]);
            }
        }

        public void TrainModel(string train_file, string output_file, string vocab_file)
        {
            if (debug_mode > 0)
            {
                //Logger.WriteLine("Starting training using file {0}", train_file);
            }

            if ((vocab_file != null && File.Exists(vocab_file) == true) ||
                strPreTrainedModelFileName != null)
            {
                if (vocab_file != null && File.Exists(vocab_file) == true)
                {
                    //Logger.WriteLine("Loading vocabulary {0} from file...", vocab_file);
                    LoadVocabFromFile(vocab_file);
                }

                if (strPreTrainedModelFileName != null)
                {
                    //Logger.WriteLine("Load vocabulary from pre-trained model file {0}", strPreTrainedModelFileName);
                    LoadVocabFromPreTrainModel(strPreTrainedModelFileName);
                }

                //Vocaburary is loaded from given dict, then we need to calculate how many words need to be train
                //Logger.WriteLine("Calculating how many words need to be train...");
                GetTrainWordSize(train_file);
                //Logger.WriteLine("Total training words : {0}", train_words);
            }
            else
            {
                //We have no input vocabulary, so we get vocabulary from training corpus
                //Logger.WriteLine("Generate vocabulary from training corpus {0}...", train_file);
                LearnVocabFromTrainFile(train_file);
            }

            //filter out words which frequenct is lower
            ShrinkVocab();

            //If vocabulary is specified in parameter list, but not existed in folder, we need to create it
            if (vocab_file != null && vocab_file.Length > 0 && File.Exists(vocab_file) == false)
            {
                if (debug_mode > 0)
                {
                    //Logger.WriteLine("Saving vocabulary into file...");
                }
                SaveVocab(vocab_file);
            }

            next_save_step = savestep;
            next_save_trained_words = next_save_step;

            if (output_file == null)
            {
                //Logger.WriteLine("No specified output file name");
                return;
            }
            //Initialize neural network
            InitNet();

            //Generate word's frequency distribution for negative samping
            InitAccTermFreq();


            //Load pre-trained model syn0
            if (strPreTrainedModelFileName != null)
            {
                //Logger.WriteLine("Loading syn0 from pre-trained model...");
                LoadPreTrainModelSyn(strPreTrainedModelFileName, syn0);

                //Logger.WriteLine("Loading syn1 from pre-trained model...");
                LoadPreTrainModelSyn(strPreTrainedModelFileName + ".syn1", syn1);
            }

            if (File.Exists(train_file) == true)
            {
                string strCurTrainFile = train_file;
                for (int j = 0; j < iter; j++)
                {
                    totalNeu_e = new double[layer1_size];

                    //Logger.WriteLine("Starting training iteration {0}/{1}...", j + 1, iter);
                    srTrainCorpus = new StreamReader(strCurTrainFile, Encoding.UTF8, true, 102400000);
                    List<Thread> threadList = new List<Thread>();
                    for (int i = 0; i < num_threads; i++)
                    {
                        Thread thread = new Thread(new ThreadStart(TrainModelThread));
                        thread.Start();
                        threadList.Add(thread);
                    }

                    //Wait all threads finish their jobs
                    for (int i = 0; i < num_threads; i++)
                    {
                        threadList[i].Join();
                    }

                    srTrainCorpus.Close();

                    double sumErr = 0;
                    for (int i = 0; i < layer1_size; i++)
                    {
                        sumErr += (totalNeu_e[i] / word_count_actual);
                    }

                    //Logger.WriteLine("Error: {0}", sumErr);
                }
            }
            else
            {
                //Logger.WriteLine("Train train file isn't existed.");
                return;
            }


            Model.SaveModel(output_file, vocab_size, layer1_size, vocab, syn0);
            Model.SaveModel(output_file + ".syn1", vocab_size, layer1_size, vocab, syn1);
        }
    }
}
