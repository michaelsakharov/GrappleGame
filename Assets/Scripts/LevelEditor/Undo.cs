using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public static class Undo
{
    /**
     * Stores a summary of the action, the target (IUndo inheriting object), and a hashtable of values representing
     * object state at time of undo snapshot.
     */
    protected class UndoState
    {
        // A summary of the undo action.
        public string message;

        // The object that is targeted by this action.
        public IUndo target;

        // A collection of values representing the state of `target`.  Will by passed to IUndo::ApplyState.
        public Hashtable values;

        /**
         * Initialize a new UndoState object with an IUndo object and summary of the undo-able action.
         */
        public UndoState(IUndo target, string msg)
        {
            this.target = target;
            this.message = msg;
            this.values = target.RecordState();
        }

        /**
         * Reverts the IUndo state by calling IUndo::ApplyState()
         */
        public void Apply()
        {
            target.ApplyState(values);
        }

        /**
         * Returns the summary of this undo action.
         */
        public override string ToString()
        {
            return message;
        }
    }

    public static event Action undoPerformed = null;
    public static event Action redoPerformed = null;
    public static event Action undoStackModified = null;
    public static event Action redoStackModified = null;

    public static bool CanUndo => undoStack.Count > 0;
    public static bool CanRedo => redoStack.Count > 0;

    /**
     * Add a callback when an Undo action is performed.
     */
    public static void AddUndoPerformedListener(Action callback)
    {
        if (undoPerformed != null)
            undoPerformed += callback;
        else
            undoPerformed = callback;
    }

    /**
     * Add a callback when an Redo action is performed.
     */
    public static void AddRedoPerformedListener(Action callback)
    {
        if (redoPerformed != null)
            redoPerformed += callback;
        else
            redoPerformed = callback;
    }

    private static Stack<List<UndoState>> undoStack = new Stack<List<UndoState>>();
    private static Stack<List<UndoState>> redoStack = new Stack<List<UndoState>>();

    static UndoState currentUndo, currentRedo;

    private static void PushUndo(List<UndoState> state)
    {
        currentUndo = state[0];
        undoStack.Push(state);

        if (undoStackModified != null)
            undoStackModified();
    }

    private static void PushRedo(List<UndoState> state)
    {
        currentRedo = state[0];
        redoStack.Push(state);

        if (redoStackModified != null)
            redoStackModified();
    }

    private static List<UndoState> PopUndo()
    {
        List<UndoState> states = Pop(undoStack);

        if (states == null || undoStack.Count < 1)
            currentUndo = null;
        else
            currentUndo = ((List<UndoState>)undoStack.Peek())[0];

        if (undoStackModified != null)
            undoStackModified();

        return states;
    }

    private static List<UndoState> PopRedo()
    {
        List<UndoState> states = Pop(redoStack);

        if (states == null || redoStack.Count < 1)
            currentRedo = null;
        else
            currentRedo = ((List<UndoState>)redoStack.Peek())[0];

        if (redoStackModified != null)
            redoStackModified();

        return states;
    }

    private static List<UndoState> Pop(Stack<List<UndoState>> stack)
    {
        if (stack.Count > 0)
            return (List<UndoState>)stack.Pop();
        else
            return null;
    }

    private static void ClearStack(Stack<List<UndoState>> stack)
    {
        foreach (List<UndoState> commands in stack)
            foreach (UndoState state in commands)
                state.target.OnExitScope();

        stack.Clear();
    }

    /**
     * Register a new undoable state with message.  Message should describe the action that will be
     * undone.
     * \sa IUndo
     */
    public static void RegisterState(IUndo target, string message)
    {
        ClearStack(redoStack);
        currentRedo = null;
        PushUndo(new List<UndoState>() { new UndoState(target, message) });
    }

    /**
     * Register a collection of undoable states with message.  Message should describe the action that
     * will be undone.
     * \sa IUndo
     */
    public static void RegisterStates(IEnumerable<IUndo> targets, string message)
    {
        ClearStack(redoStack);
        currentRedo = null;
        List<UndoState> states = targets.Select(x => new UndoState(x, message)).ToList();
        PushUndo(states);
    }

    /**
     * Applies the currently queued Undo state.
     */
    public static void PerformUndo()
    {
        List<UndoState> states = PopUndo();

        if (states == null)
            return;

        PushRedo(states.Select(x => new UndoState(x.target, x.message)).ToList());

        foreach (UndoState state in states)
            state.Apply();

        if (undoPerformed != null)
            undoPerformed();
    }

    /**
     * If the Redo stack exists, this applies the queued Redo action.  Redo is cleared on Undo.RegisterState 
     * or Undo.RegisterStates calls.
     */
    public static void PerformRedo()
    {
        List<UndoState> states = PopRedo();

        if (states == null)
            return;

        PushUndo(states.Select(x => new UndoState(x.target, x.message)).ToList());

        foreach (UndoState state in states)
            state.Apply();

        if (redoPerformed != null)
            redoPerformed();
    }
}
